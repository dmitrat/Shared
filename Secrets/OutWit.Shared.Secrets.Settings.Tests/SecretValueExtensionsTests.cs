
using OutWit.Common.Settings.Values;
using OutWit.Shared.Secrets.Providers;
using OutWit.Shared.Secrets.Settings;

namespace OutWit.Shared.Secrets.Settings.Tests
{
    [TestFixture]
    public class SecretValueExtensionsTests
    {
        #region Fields

        private SecretStoreMemory m_store = null!;

        #endregion

        #region Initialization

        [SetUp]
        public void Setup()
        {
            m_store = new SecretStoreMemory();
        }

        #endregion

        #region Round-Trip Tests

        [Test]
        public async Task SetRevealClearRoundTripTest()
        {
            var reference = new SecretValue { StoreKey = "Test.Product/ApiKey" };

            (SecretOutcome setOutcome, reference) = await reference.SetAsync(m_store, "wit_sk_nmh8P4Cq");
            Assert.That(setOutcome.IsSuccess(), Is.True, setOutcome.Message);
            Assert.That(reference.IsSet, Is.True);
            Assert.That(reference.Hint, Is.EqualTo("P4Cq"));

            SecretResult revealed = await reference.RevealAsync(m_store);
            Assert.That(revealed.Status, Is.EqualTo(SecretStatus.Found));
            Assert.That(revealed.GetString(), Is.EqualTo("wit_sk_nmh8P4Cq"));

            (SecretOutcome clearOutcome, reference) = await reference.ClearAsync(m_store);
            Assert.That(clearOutcome.IsSuccess(), Is.True);
            Assert.That(reference.IsSet, Is.False);
            Assert.That(reference.Hint, Is.EqualTo(""));

            SecretResult afterClear = await reference.RevealAsync(m_store);
            Assert.That(afterClear.Status, Is.EqualTo(SecretStatus.NotFound));
        }

        [Test]
        public async Task RevealOfUnsetReferenceIsNotFoundTest()
        {
            var reference = new SecretValue { StoreKey = "Test.Product/Absent" };

            SecretResult result = await reference.RevealAsync(m_store);

            Assert.That(result.Status, Is.EqualTo(SecretStatus.NotFound));
        }

        #endregion

        #region Failure Tests

        [Test]
        public async Task FailedSetLeavesTheReferenceUnchangedTest()
        {
            var reference = new SecretValue { StoreKey = "Test.Product/ApiKey" };

            (SecretOutcome outcome, SecretValue updated) = await reference.SetAsync(m_store, "");

            Assert.That(outcome.IsSuccess(), Is.False, "An empty secret is refused");
            Assert.That(updated, Is.SameAs(reference),
                "On failure the original reference comes back, so assignment is always safe");
            Assert.That(updated.IsSet, Is.False);
        }

        [Test]
        public void EmptyStoreKeyThrowsTest()
        {
            var reference = new SecretValue();

            Assert.ThrowsAsync<ArgumentException>(
                async () => await reference.RevealAsync(m_store));
        }

        [Test]
        public async Task ShortSecretGetsNoHintTest()
        {
            var reference = new SecretValue { StoreKey = "Test.Product/Pin" };

            (SecretOutcome outcome, reference) = await reference.SetAsync(m_store, "1234567");

            Assert.That(outcome.IsSuccess(), Is.True);
            Assert.That(reference.IsSet, Is.True);
            Assert.That(reference.Hint, Is.EqualTo(""),
                "A hint of a short secret would be the secret itself");
        }

        #endregion

    }
}
