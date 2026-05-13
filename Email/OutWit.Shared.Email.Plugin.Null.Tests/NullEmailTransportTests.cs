using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Email;
using OutWit.Shared.Email.Plugin.Null;

namespace OutWit.Shared.Email.Plugin.Null.Tests
{
    [TestFixture]
    public class NullEmailTransportTests
    {
        #region Fields

        private CapturingLogger m_logger = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_logger = new CapturingLogger();
        }

        #endregion

        #region LogOnly Tests

        [Test]
        public async Task LogOnlyModeReturnsSuccessfulResultTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.LogOnly, m_logger);
            var message = NewMessage();

            var result = await transport.SendAsync(message);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.None));
            Assert.That(result.ProviderMessageId, Is.Not.Null);
            Assert.That(result.ProviderMessageId, Does.StartWith("null-"));
        }

        [Test]
        public async Task LogOnlyModeWritesWarningContainingRecipientAndSubjectTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.LogOnly, m_logger);

            await transport.SendAsync(NewMessage(to: "user@example.com", subject: "Verify your email"));

            Assert.That(m_logger.Entries, Has.Count.EqualTo(1));
            Assert.That(m_logger.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(m_logger.Entries[0].Message, Does.Contain("user@example.com"));
            Assert.That(m_logger.Entries[0].Message, Does.Contain("Verify your email"));
        }

        [Test]
        public async Task LogOnlyModeTruncatesLongBodyExcerptTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.LogOnly, m_logger);
            var longBody = new string('x', 5000);

            await transport.SendAsync(NewMessage(htmlBody: longBody));

            // Excerpt is capped at 200 chars + "…" suffix.
            Assert.That(m_logger.Entries[0].Message.Length, Is.LessThan(longBody.Length));
        }

        [Test]
        public async Task LogOnlyModeUsesOnlyFirstLineOfBodyForExcerptTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.LogOnly, m_logger);

            await transport.SendAsync(NewMessage(htmlBody: "first line\nsecond line\nthird line"));

            Assert.That(m_logger.Entries[0].Message, Does.Contain("first line"));
            Assert.That(m_logger.Entries[0].Message, Does.Not.Contain("second line"));
        }

        #endregion

        #region Drop Tests

        [Test]
        public async Task DropModeReturnsPermanentFailureTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.Drop, m_logger);
            var message = NewMessage();

            var result = await transport.SendAsync(message);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(EmailFailureKind.Permanent));
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task DropModeLogsAtErrorLevelTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.Drop, m_logger);

            await transport.SendAsync(NewMessage(to: "user@example.com"));

            Assert.That(m_logger.Entries, Has.Count.EqualTo(1));
            Assert.That(m_logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(m_logger.Entries[0].Message, Does.Contain("user@example.com"));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void NullMessageThrowsArgumentNullExceptionTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.LogOnly, m_logger);

            Assert.Throws<ArgumentNullException>(() => transport.SendAsync(null!).GetAwaiter().GetResult());
        }

        [Test]
        public void ModeIsExposedAsPropertyTest()
        {
            var transport = new NullEmailTransport(NullEmailMode.Drop, m_logger);

            Assert.That(transport.Mode, Is.EqualTo(NullEmailMode.Drop));
        }

        #endregion

        #region Helpers

        private static EmailMessage NewMessage(string to = "to@example.com",
            string from = "from@example.com",
            string subject = "Test",
            string htmlBody = "<p>Test</p>")
        {
            return new EmailMessage
            {
                To = to,
                From = from,
                Subject = subject,
                HtmlBody = htmlBody
            };
        }

        private sealed class CapturingLogger : ILogger<NullEmailTransport>
        {
            public List<(LogLevel Level, string Message)> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }
        }

        #endregion
    }
}
