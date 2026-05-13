using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Resend;
using EmailMessage = OutWit.Common.Email.EmailMessage;

namespace OutWit.Shared.Email.Provider.Resend.Tests
{
    /// <summary>
    /// Real Resend API integration. Marked <see cref="ExplicitAttribute"/> so it
    /// is skipped by default in CI. Set <c>Resend__ApiToken</c>,
    /// <c>Resend_TestFrom</c>, and <c>Resend_TestTo</c> env vars before running.
    /// </summary>
    [TestFixture]
    [Explicit("Hits the live Resend API — set Resend__ApiToken / Resend_TestFrom / Resend_TestTo and run manually.")]
    [Category("Resend")]
    public class ResendLiveIntegrationTests
    {
        #region Tests

        [Test]
        public async Task SendsLiveEmailTest()
        {
            var apiToken = Environment.GetEnvironmentVariable("Resend__ApiToken");
            var from     = Environment.GetEnvironmentVariable("Resend_TestFrom");
            var to       = Environment.GetEnvironmentVariable("Resend_TestTo");

            Assert.That(apiToken, Is.Not.Null.And.Not.Empty, "Resend__ApiToken env var is required.");
            Assert.That(from,     Is.Not.Null.And.Not.Empty, "Resend_TestFrom env var is required.");
            Assert.That(to,       Is.Not.Null.And.Not.Empty, "Resend_TestTo env var is required.");

            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            // Drive the real plugin so the wiring (HttpClient + IOptions + IResend) matches production.
            var plugin = new ResendEmailProviderPlugin();
            plugin.Initialize(services);

            await using var sp = services.BuildServiceProvider();
            var transport = sp.GetRequiredService<IEmailTransport>();

            var result = await transport.SendAsync(new EmailMessage
            {
                To       = to!,
                From     = from!,
                Subject  = $"OutWit Resend integration test {DateTime.UtcNow:O}",
                HtmlBody = "<p>This message was sent by OutWit.Shared.Email.Provider.Resend.Tests.</p>"
            });

            Assert.That(result.Succeeded, Is.True,
                $"Send failed ({result.FailureKind}): {result.ErrorMessage}");
            Assert.That(result.ProviderMessageId, Is.Not.Null.And.Not.Empty);
        }

        #endregion
    }
}
