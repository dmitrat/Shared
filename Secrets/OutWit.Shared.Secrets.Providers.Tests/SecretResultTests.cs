using System.Text;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Providers.Tests
{
    [TestFixture]
    public class SecretResultTests
    {
        #region Logging Tests

        [Test]
        public void ToStringDoesNotContainSecretTest()
        {
            var result = new SecretResult
            {
                Status = SecretStatus.Found,
                Secret = Encoding.UTF8.GetBytes("TOPSECRET-VALUE"),
                Message = "found it"
            };

            string text = result.ToString();

            Assert.That(text, Does.Not.Contain("TOPSECRET"));
            Assert.That(text, Does.Contain(nameof(SecretStatus.Found)));
            Assert.That(text, Does.Contain("found it"));
        }

        [Test]
        public void DescriptionToStringNamesProtectionTest()
        {
            var description = new SecretStoreDescription
            {
                Key = "Windows",
                Protection = SecretProtection.OperatingSystem,
                CanWrite = true,
                Location = "vault"
            };

            string text = description.ToString();

            Assert.That(text, Does.Contain(nameof(SecretProtection.OperatingSystem)));
            Assert.That(text, Does.Contain("Windows"));
        }

        #endregion

        #region Model Tests

        [Test]
        public void IsComparesSecretBytesTest()
        {
            var left = new SecretResult { Status = SecretStatus.Found, Secret = new byte[] { 1, 2, 3 } };
            var same = new SecretResult { Status = SecretStatus.Found, Secret = new byte[] { 1, 2, 3 } };
            var other = new SecretResult { Status = SecretStatus.Found, Secret = new byte[] { 1, 2, 4 } };
            var missing = new SecretResult { Status = SecretStatus.NotFound };

            Assert.That(left.Is(same), Is.True);
            Assert.That(left.Is(other), Is.False);
            Assert.That(left.Is(missing), Is.False);
            Assert.That(missing.Is(new SecretResult { Status = SecretStatus.NotFound }), Is.True);
        }

        [Test]
        public void CloneCopiesSecretBytesTest()
        {
            var original = new SecretResult { Status = SecretStatus.Found, Secret = new byte[] { 1, 2, 3 } };

            var clone = (SecretResult)original.Clone();
            original.Secret![0] = 99;

            Assert.That(clone.Secret![0], Is.EqualTo(1));
            Assert.That(clone.Is(original), Is.False);
        }

        [Test]
        public void DefaultStatusIsUnknownTest()
        {
            Assert.That(new SecretResult().Status, Is.EqualTo(SecretStatus.Unknown));
            Assert.That(new SecretOutcome().Status, Is.EqualTo(SecretStatus.Unknown));
        }

        #endregion
    }
}
