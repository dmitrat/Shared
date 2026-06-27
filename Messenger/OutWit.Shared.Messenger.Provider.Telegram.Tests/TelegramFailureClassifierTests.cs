using System;
using System.Net.Http;
using OutWit.Common.Messenger;
using OutWit.Shared.Messenger.Provider.Telegram;

namespace OutWit.Shared.Messenger.Provider.Telegram.Tests
{
    [TestFixture]
    public class TelegramFailureClassifierTests
    {
        #region Classify(Exception) Tests

        [Test]
        public void HttpRequestExceptionIsTransientTest()
        {
            Assert.That(TelegramFailureClassifier.Classify(new HttpRequestException("x")),
                Is.EqualTo(MessengerFailureKind.Transient));
        }

        [Test]
        public void TaskCanceledExceptionIsTransientTest()
        {
            Assert.That(TelegramFailureClassifier.Classify(new TaskCanceledException()),
                Is.EqualTo(MessengerFailureKind.Transient));
        }

        [Test]
        public void NestedHttpRequestExceptionIsTransientTest()
        {
            var ex = new InvalidOperationException("wrapper", new HttpRequestException("inner"));

            Assert.That(TelegramFailureClassifier.Classify(ex),
                Is.EqualTo(MessengerFailureKind.Transient));
        }

        [Test]
        public void UnknownExceptionIsPermanentTest()
        {
            Assert.That(TelegramFailureClassifier.Classify(new InvalidOperationException("x")),
                Is.EqualTo(MessengerFailureKind.Permanent));
        }

        #endregion

        #region ClassifyResponse Tests

        [Test]
        public void Status401IsAuthFailureTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(401, "Unauthorized"),
                Is.EqualTo(MessengerFailureKind.AuthFailure));
        }

        [Test]
        public void Status403IsInvalidRecipientTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(403, "Forbidden"),
                Is.EqualTo(MessengerFailureKind.InvalidRecipient));
        }

        [Test]
        public void Status429IsRateLimitedTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(429, "Too Many Requests"),
                Is.EqualTo(MessengerFailureKind.RateLimited));
        }

        [Test]
        public void Status400WithoutKnownDescriptionIsPermanentTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(400, "Bad Request: message text is empty"),
                Is.EqualTo(MessengerFailureKind.Permanent));
        }

        [Test]
        public void Status400ChatNotFoundIsInvalidRecipientTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(400, "Bad Request: chat not found"),
                Is.EqualTo(MessengerFailureKind.InvalidRecipient));
        }

        [Test]
        public void BotBlockedIsInvalidRecipientTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(403, "Forbidden: bot was blocked by the user"),
                Is.EqualTo(MessengerFailureKind.InvalidRecipient));
        }

        [Test]
        public void Status500IsTransientTest()
        {
            Assert.That(TelegramFailureClassifier.ClassifyResponse(502, "Bad Gateway"),
                Is.EqualTo(MessengerFailureKind.Transient));
        }

        #endregion
    }
}
