using System.Text;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Providers.Tests
{
    [TestFixture]
    public class SecretStoreBaseTests
    {
        #region Read-Only Tests

        [Test]
        public async Task StoreOnReadOnlyStoreIsDeniedTest()
        {
            var store = new SecretStoreMemory(canWrite: false);

            SecretOutcome outcome = await store.StoreAsync("Test/Key", new byte[] { 1, 2, 3 });

            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Denied));
            Assert.That(outcome.Message, Does.Contain("read-only"));
        }

        [Test]
        public async Task DeleteOnReadOnlyStoreIsDeniedTest()
        {
            var store = new SecretStoreMemory(canWrite: false);

            SecretOutcome outcome = await store.DeleteAsync("Test/Key");

            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Denied));
        }

        [Test]
        public async Task ReadOnReadOnlyStoreWorksTest()
        {
            var store = new SecretStoreMemory(canWrite: false);
            byte[] secret = Encoding.UTF8.GetBytes("seeded");
            store.Seed("Test/Key", secret);

            SecretResult found = await store.ReadAsync("Test/Key");
            SecretResult missing = await store.ReadAsync("Test/Absent");

            Assert.That(found.Status, Is.EqualTo(SecretStatus.Found));
            Assert.That(found.Secret, Is.EqualTo(secret));
            Assert.That(missing.Status, Is.EqualTo(SecretStatus.NotFound));
        }

        #endregion

        #region Cancellation Tests

        [Test]
        public void CancelledTokenThrowsTest()
        {
            var store = new SecretStoreMemory();
            using var source = new CancellationTokenSource();
            source.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(
                async () => await store.ReadAsync("Test/Key", source.Token));
            Assert.ThrowsAsync<OperationCanceledException>(
                async () => await store.StoreAsync("Test/Key", new byte[] { 1 }, source.Token));
            Assert.ThrowsAsync<OperationCanceledException>(
                async () => await store.DeleteAsync("Test/Key", source.Token));
        }

        #endregion

        #region Extension Tests

        [Test]
        public async Task StoreStringRoundTripsThroughGetStringTest()
        {
            var store = new SecretStoreMemory();

            SecretOutcome outcome = await store.StoreStringAsync("Test/Key", "пароль-π-🔑");
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Found));

            SecretResult result = await store.ReadAsync("Test/Key");
            Assert.That(result.GetString(), Is.EqualTo("пароль-π-🔑"));
        }

        [Test]
        public async Task GetStringOnNotFoundIsNullTest()
        {
            var store = new SecretStoreMemory();

            SecretResult result = await store.ReadAsync("Test/Absent");

            Assert.That(result.GetString(), Is.Null);
        }

        #endregion
    }
}
