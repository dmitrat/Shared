using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Smtp;

namespace OutWit.Shared.Email.Provider.Smtp.Tests
{
    /// <summary>
    /// End-to-end tests against a real SMTP catcher running on <c>localhost:1025</c>.
    /// All tests are <c>[Explicit]</c> — they require <a href="https://github.com/mailhog/MailHog">MailHog</a>
    /// or <a href="https://github.com/ChangemakerStudios/Papercut-SMTP">Papercut-SMTP</a> to be running:
    /// <code>
    /// docker run -d -p 1025:1025 -p 8025:8025 --name mailhog mailhog/mailhog
    /// </code>
    /// Run via: <c>dotnet test --filter Category=MailHog</c>
    /// </summary>
    [TestFixture, Category("MailHog"), Explicit("Requires SMTP catcher on localhost:1025")]
    public class MailHogIntegrationTests
    {
        private SmtpEmailTransport m_transport = null!;

        [SetUp]
        public void Setup()
        {
            var options = new SmtpOptions
            {
                Host = "localhost",
                Port = 1025,
                Security = SmtpSecurity.None,
                Timeout = TimeSpan.FromSeconds(5)
            };
            m_transport = new SmtpEmailTransport(options, NullLogger<SmtpEmailTransport>.Instance);
        }

        [Test]
        public async Task SimpleHtmlEmailDeliversTest()
        {
            var result = await m_transport.SendAsync(new EmailMessage
            {
                To = "alice@example.com",
                From = "bot@example.com",
                Subject = "Integration test — simple HTML",
                HtmlBody = "<p>Hello from <strong>SmtpEmailTransport</strong>.</p>"
            });

            Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.None));
        }

        [Test]
        public async Task HtmlPlusTextAlternativeDeliversTest()
        {
            var result = await m_transport.SendAsync(new EmailMessage
            {
                To = "alice@example.com",
                From = "bot@example.com",
                Subject = "Integration test — html + text alt",
                HtmlBody = "<p>HTML body.</p>",
                TextBody = "Plain text body."
            });

            Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
        }

        [Test]
        public async Task CustomHeadersAreIncludedTest()
        {
            var result = await m_transport.SendAsync(new EmailMessage
            {
                To = "alice@example.com",
                From = "bot@example.com",
                Subject = "Integration test — custom headers",
                HtmlBody = "<p>Body.</p>",
                Headers = new Dictionary<string, string>
                {
                    ["X-Custom-Tag"] = "smoke-test",
                    ["X-Trace-Id"] = Guid.NewGuid().ToString()
                }
            });

            Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
        }

        [Test]
        public async Task ConnectionFailureReturnsTransientFailureTest()
        {
            // Point at a port that's definitely closed.
            var transport = new SmtpEmailTransport(
                new SmtpOptions
                {
                    Host = "localhost",
                    Port = 1, // closed
                    Security = SmtpSecurity.None,
                    Timeout = TimeSpan.FromSeconds(1)
                },
                NullLogger<SmtpEmailTransport>.Instance);

            var result = await transport.SendAsync(new EmailMessage
            {
                To = "alice@example.com",
                From = "bot@example.com",
                Subject = "Should never arrive",
                HtmlBody = "<p>Will not be delivered.</p>"
            });

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.Transient));
        }
    }
}
