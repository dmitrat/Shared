using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Providers.Tests
{
    [TestFixture]
    public class SecretKeysTests
    {
        #region Validation Tests

        [Test]
        public void ValidateAcceptsConventionKeyTest()
        {
            Assert.DoesNotThrow(() => SecretKeys.Validate("Norav.Bridge/AgentCredential"));
            Assert.DoesNotThrow(() => SecretKeys.Validate("a"));
            Assert.DoesNotThrow(() => SecretKeys.Validate("A-b_c.d/e-1"));
            Assert.DoesNotThrow(() => SecretKeys.Validate(new string('x', SecretKeys.MAX_LENGTH)));
        }

        [Test]
        public void ValidateRejectsNullKeyTest()
        {
            Assert.Throws<ArgumentNullException>(() => SecretKeys.Validate(null!));
        }

        [Test]
        public void ValidateRejectsEmptyKeyTest()
        {
            Assert.Throws<ArgumentException>(() => SecretKeys.Validate(""));
        }

        [Test]
        public void ValidateRejectsOverlongKeyTest()
        {
            Assert.Throws<ArgumentException>(
                () => SecretKeys.Validate(new string('x', SecretKeys.MAX_LENGTH + 1)));
        }

        [Test]
        public void ValidateRejectsInvalidCharactersTest()
        {
            Assert.Throws<ArgumentException>(() => SecretKeys.Validate("has space"));
            Assert.Throws<ArgumentException>(() => SecretKeys.Validate("has\\backslash"));
            Assert.Throws<ArgumentException>(() => SecretKeys.Validate("has:colon"));
            Assert.Throws<ArgumentException>(() => SecretKeys.Validate("hasумлаут"));
            Assert.Throws<ArgumentException>(() => SecretKeys.Validate("has#hash"));
        }

        [Test]
        public void IsValidAgreesWithValidateTest()
        {
            Assert.That(SecretKeys.IsValid("Norav.Bridge/AgentCredential"), Is.True);
            Assert.That(SecretKeys.IsValid(null), Is.False);
            Assert.That(SecretKeys.IsValid(""), Is.False);
            Assert.That(SecretKeys.IsValid("has space"), Is.False);
            Assert.That(SecretKeys.IsValid(new string('x', SecretKeys.MAX_LENGTH + 1)), Is.False);
        }

        #endregion
    }
}
