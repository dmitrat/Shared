using System;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Shared.Email.Provider.Smtp;

namespace OutWit.Shared.Email.Provider.Smtp.Tests
{
    [TestFixture]
    public class SmtpEmailTransportTests
    {
        #region Construction validation

        [Test]
        public void ConstructorThrowsWhenHostIsEmptyTest()
        {
            var options = new SmtpOptions(); // Host defaults to ""
            Assert.Throws<InvalidOperationException>(
                () => new SmtpEmailTransport(options, NullLogger<SmtpEmailTransport>.Instance));
        }

        [Test]
        public void ConstructorThrowsOnNullOptionsTest()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SmtpEmailTransport(null!, NullLogger<SmtpEmailTransport>.Instance));
        }

        [Test]
        public void ConstructorThrowsOnNullLoggerTest()
        {
            Assert.Throws<ArgumentNullException>(
                () => new SmtpEmailTransport(new SmtpOptions { Host = "smtp.example.com" }, null!));
        }

        [Test]
        public void ConstructorSucceedsWithValidHostTest()
        {
            var transport = new SmtpEmailTransport(
                new SmtpOptions { Host = "smtp.example.com" },
                NullLogger<SmtpEmailTransport>.Instance);

            Assert.That(transport, Is.Not.Null);
        }

        #endregion
    }
}
