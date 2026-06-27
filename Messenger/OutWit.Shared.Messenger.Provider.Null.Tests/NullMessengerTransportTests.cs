using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OutWit.Common.Messenger;
using OutWit.Shared.Messenger.Provider.Null;

namespace OutWit.Shared.Messenger.Provider.Null.Tests
{
    [TestFixture]
    public class NullMessengerTransportTests
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
            var transport = new NullMessengerTransport(NullMessengerMode.LogOnly, m_logger);

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.None));
            Assert.That(result.ProviderMessageId, Is.Not.Null);
            Assert.That(result.ProviderMessageId, Does.StartWith("null-"));
        }

        [Test]
        public async Task LogOnlyModeWritesWarningContainingTargetAndTextTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.LogOnly, m_logger);

            await transport.SendAsync(NewMessage(target: "123456", text: "Order filled: AAPL"));

            Assert.That(m_logger.Entries, Has.Count.EqualTo(1));
            Assert.That(m_logger.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(m_logger.Entries[0].Message, Does.Contain("123456"));
            Assert.That(m_logger.Entries[0].Message, Does.Contain("Order filled: AAPL"));
        }

        [Test]
        public async Task LogOnlyModeTruncatesLongTextExcerptTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.LogOnly, m_logger);
            var longText = new string('x', 5000);

            await transport.SendAsync(NewMessage(text: longText));

            Assert.That(m_logger.Entries[0].Message.Length, Is.LessThan(longText.Length));
        }

        [Test]
        public async Task LogOnlyModeUsesOnlyFirstLineOfTextForExcerptTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.LogOnly, m_logger);

            await transport.SendAsync(NewMessage(text: "first line\nsecond line\nthird line"));

            Assert.That(m_logger.Entries[0].Message, Does.Contain("first line"));
            Assert.That(m_logger.Entries[0].Message, Does.Not.Contain("second line"));
        }

        #endregion

        #region Drop Tests

        [Test]
        public async Task DropModeReturnsPermanentFailureTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.Drop, m_logger);

            var result = await transport.SendAsync(NewMessage());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(MessengerFailureKind.Permanent));
            Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task DropModeLogsAtErrorLevelTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.Drop, m_logger);

            await transport.SendAsync(NewMessage(target: "123456"));

            Assert.That(m_logger.Entries, Has.Count.EqualTo(1));
            Assert.That(m_logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(m_logger.Entries[0].Message, Does.Contain("123456"));
        }

        #endregion

        #region Validation Tests

        [Test]
        public void NullMessageThrowsArgumentNullExceptionTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.LogOnly, m_logger);

            Assert.Throws<ArgumentNullException>(() => transport.SendAsync(null!).GetAwaiter().GetResult());
        }

        [Test]
        public void ModeIsExposedAsPropertyTest()
        {
            var transport = new NullMessengerTransport(NullMessengerMode.Drop, m_logger);

            Assert.That(transport.Mode, Is.EqualTo(NullMessengerMode.Drop));
        }

        #endregion

        #region Helpers

        private static MessengerMessage NewMessage(string target = "chat-1", string text = "Test")
        {
            return new MessengerMessage
            {
                Target = target,
                Text = text
            };
        }

        private sealed class CapturingLogger : ILogger<NullMessengerTransport>
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
