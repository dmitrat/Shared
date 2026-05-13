using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OutWit.Shared.Email.Provider.Resend;
using Resend;
using EmailFailureKind = OutWit.Common.Email.EmailFailureKind;
using EmailMessage = OutWit.Common.Email.EmailMessage;
using ResendClientOptions = Resend.ResendClientOptions;

namespace OutWit.Shared.Email.Provider.Resend.Tests
{
    [TestFixture]
    public class ResendEmailTransportTests
    {
        #region Construction validation

        [Test]
        public void ConstructorThrowsOnNullResendTest()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ResendEmailTransport(null!, NullLogger<ResendEmailTransport>.Instance));
        }

        [Test]
        public void ConstructorThrowsOnNullLoggerTest()
        {
            var resend = BuildResendClient(new HttpMessageHandlerStub(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)));
            Assert.Throws<ArgumentNullException>(() => new ResendEmailTransport(resend, null!));
        }

        #endregion

        #region SendAsync — happy path

        [Test]
        public async Task SendAsyncReturnsSuccessWithMessageIdOnHttp200Test()
        {
            var messageId = "5d34c2ef-1d36-4c4c-b6e9-aaaaaaaaaaaa";
            var responseJson = $"{{\"id\":\"{messageId}\"}}";

            var handler = new HttpMessageHandlerStub(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            var transport = new ResendEmailTransport(
                BuildResendClient(handler),
                NullLogger<ResendEmailTransport>.Instance);

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.None));
            Assert.That(result.ProviderMessageId, Is.EqualTo(messageId));
        }

        [Test]
        public async Task SendAsyncIssuesSinglePostRequestTest()
        {
            int callCount = 0;
            var handler = new HttpMessageHandlerStub(req =>
            {
                callCount++;
                Assert.That(req.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(req.RequestUri!.AbsolutePath, Does.Contain("/emails"));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"00000000-0000-0000-0000-000000000000\"}",
                        Encoding.UTF8, "application/json")
                };
            });

            var transport = new ResendEmailTransport(
                BuildResendClient(handler),
                NullLogger<ResendEmailTransport>.Instance);

            await transport.SendAsync(NewMessage());

            Assert.That(callCount, Is.EqualTo(1));
        }

        #endregion

        #region SendAsync — failure mapping

        [Test]
        public async Task SendAsyncReturnsAuthFailureOn401Test()
        {
            var transport = TransportFor(HttpStatusCode.Unauthorized,
                "{\"name\":\"missing_api_key\",\"message\":\"Missing API key\"}");

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.AuthFailure));
        }

        [Test]
        public async Task SendAsyncReturnsRateLimitedOn429Test()
        {
            var transport = TransportFor(HttpStatusCode.TooManyRequests,
                "{\"name\":\"rate_limit_exceeded\",\"message\":\"Too many requests\"}");

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.RateLimited));
        }

        [Test]
        public async Task SendAsyncReturnsTransientOn503Test()
        {
            var transport = TransportFor(HttpStatusCode.ServiceUnavailable,
                "{\"name\":\"application_error\",\"message\":\"Service unavailable\"}");

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public async Task SendAsyncReturnsPermanentOn422ValidationErrorTest()
        {
            var transport = TransportFor(HttpStatusCode.UnprocessableEntity,
                "{\"name\":\"validation_error\",\"message\":\"Invalid From\"}");

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        [Test]
        public async Task SendAsyncReturnsTransientOnHttpRequestExceptionTest()
        {
            var handler = new HttpMessageHandlerStub(_ =>
                throw new HttpRequestException("connection refused"));

            var transport = new ResendEmailTransport(
                BuildResendClient(handler),
                NullLogger<ResendEmailTransport>.Instance);

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.Transient));
        }

        #endregion

        #region Tools

        private static EmailMessage NewMessage() => new()
        {
            From = "bot@example.com",
            To = "alice@example.com",
            Subject = "Test",
            HtmlBody = "<p>hi</p>"
        };

        private static ResendEmailTransport TransportFor(HttpStatusCode status, string body)
        {
            var handler = new HttpMessageHandlerStub(_ =>
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            return new ResendEmailTransport(
                BuildResendClient(handler),
                NullLogger<ResendEmailTransport>.Instance);
        }

        private static ResendClient BuildResendClient(HttpMessageHandler handler)
        {
            var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com") };
            var snapshot = new TestOptionsSnapshot<ResendClientOptions>(new ResendClientOptions
            {
                ApiToken = "re_test",
                ThrowExceptions = true
            });
            return new ResendClient(snapshot, http);
        }

        private sealed class TestOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class
        {
            private readonly T m_value;
            public TestOptionsSnapshot(T value) { m_value = value; }
            public T Value => m_value;
            public T Get(string? name) => m_value;
        }

        private sealed class HttpMessageHandlerStub : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> m_responder;

            public HttpMessageHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                m_responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(m_responder(request));
            }
        }

        #endregion
    }
}
