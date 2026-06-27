using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Messenger;
using OutWit.Shared.Messenger.Provider.Telegram;

namespace OutWit.Shared.Messenger.Provider.Telegram.Tests
{
    [TestFixture]
    public class TelegramMessengerTransportTests
    {
        #region Success Tests

        [Test]
        public async Task SuccessfulSendReturnsMessageIdTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK,
                "{\"ok\":true,\"result\":{\"message_id\":42}}");
            var transport = NewTransport(handler, token: "tok", defaultChat: "999");

            var result = await transport.SendAsync(new MessengerMessage { Target = "123", Text = "hi" });

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.None));
            Assert.That(result.ProviderMessageId, Is.EqualTo("42"));
        }

        [Test]
        public async Task RequestTargetsConfiguredTokenAndChatTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true,\"result\":{\"message_id\":1}}");
            var transport = NewTransport(handler, token: "secret-token");

            await transport.SendAsync(new MessengerMessage { Target = "555", Text = "body" });

            Assert.That(handler.LastRequestUri, Does.Contain("/botsecret-token/sendMessage"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"chat_id\":\"555\""));
            Assert.That(handler.LastRequestBody, Does.Contain("body"));
        }

        [Test]
        public async Task EmptyTargetFallsBackToDefaultChatIdTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true,\"result\":{\"message_id\":1}}");
            var transport = NewTransport(handler, token: "tok", defaultChat: "777");

            await transport.SendAsync(new MessengerMessage { Target = "", Text = "x" });

            Assert.That(handler.LastRequestBody, Does.Contain("\"chat_id\":\"777\""));
        }

        [Test]
        public async Task MarkdownFormatSendsMarkdownV2ParseModeTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true,\"result\":{\"message_id\":1}}");
            var transport = NewTransport(handler, token: "tok");

            await transport.SendAsync(new MessengerMessage
            {
                Target = "1", Text = "x", Format = MessageFormat.Markdown
            });

            Assert.That(handler.LastRequestBody, Does.Contain("MarkdownV2"));
        }

        [Test]
        public async Task SendErrorOverloadPrependsErrorEmojiInRequestBodyTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true,\"result\":{\"message_id\":1}}");
            var transport = NewTransport(handler, token: "tok");

            await transport.SendErrorAsync("123", "Buy failed");

            Assert.That(handler.LastRequestBody, Does.Contain("Buy failed"));
            Assert.That(handler.LastRequestBody, Does.Contain(MessageEmoji.Error));
        }

        [Test]
        public async Task PlainFormatOmitsParseModeTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true,\"result\":{\"message_id\":1}}");
            var transport = NewTransport(handler, token: "tok");

            await transport.SendAsync(new MessengerMessage { Target = "1", Text = "x" });

            Assert.That(handler.LastRequestBody, Does.Not.Contain("parse_mode"));
        }

        #endregion

        #region Failure Tests

        [Test]
        public async Task MissingTokenReturnsAuthFailureWithoutHttpCallTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true}");
            var transport = NewTransport(handler, token: "");

            var result = await transport.SendAsync(new MessengerMessage { Target = "1", Text = "x" });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.AuthFailure));
            Assert.That(handler.LastRequestUri, Is.Null);
        }

        [Test]
        public async Task MissingTargetAndDefaultReturnsInvalidRecipientTest()
        {
            var handler = new StubHandler(HttpStatusCode.OK, "{\"ok\":true}");
            var transport = NewTransport(handler, token: "tok", defaultChat: "");

            var result = await transport.SendAsync(new MessengerMessage { Target = "", Text = "x" });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.InvalidRecipient));
            Assert.That(handler.LastRequestUri, Is.Null);
        }

        [Test]
        public async Task ChatNotFoundResponseReturnsInvalidRecipientTest()
        {
            var handler = new StubHandler(HttpStatusCode.BadRequest,
                "{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: chat not found\"}");
            var transport = NewTransport(handler, token: "tok");

            var result = await transport.SendAsync(new MessengerMessage { Target = "1", Text = "x" });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.InvalidRecipient));
            Assert.That(result.ErrorMessage, Does.Contain("chat not found"));
        }

        [Test]
        public async Task RateLimitedResponseReturnsRateLimitedTest()
        {
            var handler = new StubHandler((HttpStatusCode)429,
                "{\"ok\":false,\"error_code\":429,\"description\":\"Too Many Requests\",\"parameters\":{\"retry_after\":30}}");
            var transport = NewTransport(handler, token: "tok");

            var result = await transport.SendAsync(new MessengerMessage { Target = "1", Text = "x" });

            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.RateLimited));
        }

        [Test]
        public async Task NetworkExceptionReturnsTransientTest()
        {
            var transport = NewTransport(new ThrowingHandler(), token: "tok");

            var result = await transport.SendAsync(new MessengerMessage { Target = "1", Text = "x" });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.Transient));
        }

        [Test]
        public void NullMessageThrowsArgumentNullExceptionTest()
        {
            var transport = NewTransport(new StubHandler(HttpStatusCode.OK, "{}"), token: "tok");

            Assert.Throws<ArgumentNullException>(() => transport.SendAsync(null!).GetAwaiter().GetResult());
        }

        #endregion

        #region Helpers

        private static TelegramMessengerTransport NewTransport(HttpMessageHandler handler,
            string token, string? defaultChat = null)
        {
            return new TelegramMessengerTransport(
                new HttpClient(handler),
                new TelegramOptions { BotToken = token, DefaultChatId = defaultChat },
                NullLogger<TelegramMessengerTransport>.Instance);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode m_status;
            private readonly string m_body;

            public StubHandler(HttpStatusCode status, string body)
            {
                m_status = status;
                m_body = body;
            }

            public string? LastRequestUri { get; private set; }

            public string? LastRequestBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestUri = request.RequestUri?.ToString();
                if (request.Content != null)
                    LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(m_status)
                {
                    Content = new StringContent(m_body, Encoding.UTF8, "application/json")
                };
            }
        }

        private sealed class ThrowingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("network down");
            }
        }

        #endregion
    }
}
