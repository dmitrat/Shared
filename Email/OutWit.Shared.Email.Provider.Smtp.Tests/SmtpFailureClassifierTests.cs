using System;
using System.IO;
using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Smtp;

namespace OutWit.Shared.Email.Provider.Smtp.Tests
{
    [TestFixture]
    public class SmtpFailureClassifierTests
    {
        #region Exception → Kind

        [Test]
        public void AuthenticationExceptionMapsToAuthFailureTest()
        {
            var kind = SmtpFailureClassifier.Classify(new AuthenticationException("bad creds"));
            Assert.That(kind, Is.EqualTo(EmailFailureKind.AuthFailure));
        }

        [Test]
        public void IOExceptionMapsToTransientTest()
        {
            var kind = SmtpFailureClassifier.Classify(new IOException("network blip"));
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void SocketExceptionMapsToTransientTest()
        {
            var kind = SmtpFailureClassifier.Classify(new SocketException());
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void TimeoutExceptionMapsToTransientTest()
        {
            var kind = SmtpFailureClassifier.Classify(new TimeoutException("server slow"));
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void OperationCanceledMapsToTransientTest()
        {
            var kind = SmtpFailureClassifier.Classify(new OperationCanceledException());
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void UnknownExceptionMapsToPermanentTest()
        {
            var kind = SmtpFailureClassifier.Classify(new InvalidOperationException("something else"));
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        #endregion

        #region Status code → Kind

        [TestCase(421)]
        [TestCase(450)]
        [TestCase(451)]
        [TestCase(452)]
        public void TransientStatusCodesMapToTransientTest(int code)
        {
            var kind = SmtpFailureClassifier.ClassifyStatusCode(code);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void StatusCode535MapsToAuthFailureTest()
        {
            var kind = SmtpFailureClassifier.ClassifyStatusCode(535);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.AuthFailure));
        }

        [TestCase(550)]
        [TestCase(551)]
        [TestCase(553)]
        public void BadRecipientStatusCodesMapToInvalidRecipientTest(int code)
        {
            var kind = SmtpFailureClassifier.ClassifyStatusCode(code);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.InvalidRecipient));
        }

        [Test]
        public void StatusCode552MapsToPermanentMessageTooLargeTest()
        {
            // 552 = "Requested mail action aborted: exceeded storage allocation"
            var kind = SmtpFailureClassifier.ClassifyStatusCode(552);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        [TestCase(500)]   // syntax error
        [TestCase(501)]   // syntax error in parameters
        [TestCase(503)]   // bad sequence of commands
        [TestCase(554)]   // transaction failed
        public void OtherFiveXxStatusCodesMapToPermanentTest(int code)
        {
            var kind = SmtpFailureClassifier.ClassifyStatusCode(code);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        #endregion
    }
}
