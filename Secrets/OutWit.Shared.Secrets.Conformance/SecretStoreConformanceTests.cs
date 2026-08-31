using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Conformance
{
    /// <summary>
    /// The abstraction's promises, tested against every implementation by that
    /// implementation's own author. A provider's test project inherits this fixture and
    /// implements <see cref="CreateStore"/>; provider-specific properties (a Windows entry's
    /// target name, a file's permissions, Linux behaviour with no session bus) belong in the
    /// provider's own tests, not here. A provider whose <see cref="SecretStoreDescription.CanWrite"/>
    /// is false runs the read-side tests only; the mutation tests are skipped, not failed.
    /// </summary>
    public abstract class SecretStoreConformanceTests
    {
        #region Fields

        private ISecretStore m_store = null!;

        private List<string> m_usedKeys = null!;

        #endregion

        #region Initialization

        [SetUp]
        public void SetupConformance()
        {
            m_store = CreateStore();
            m_usedKeys = new List<string>();
        }

        [TearDown]
        public async Task TeardownConformance()
        {
            if (!m_store.Description.CanWrite)
                return;

            foreach (string key in m_usedKeys)
                await m_store.DeleteAsync(key);
        }

        /// <summary>
        /// Creates the store under test. Called once per test.
        /// </summary>
        /// <returns>The store.</returns>
        protected abstract ISecretStore CreateStore();

        #endregion

        #region Round-Trip Tests

        [Test]
        public async Task StoreThenReadReturnsSameBytesTest()
        {
            RequireWrite();
            string key = NewKey();
            byte[] secret = RandomSecret(64);

            SecretOutcome outcome = await m_store.StoreAsync(key, secret);
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Found), outcome.Message);

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.Found), result.Message);
            Assert.That(result.Secret, Is.EqualTo(secret));
        }

        [Test]
        public async Task StoreOverExistingKeyReplacesTest()
        {
            RequireWrite();
            string key = NewKey();
            byte[] first = RandomSecret(48);
            byte[] second = RandomSecret(96);

            await m_store.StoreAsync(key, first);
            SecretOutcome outcome = await m_store.StoreAsync(key, second);
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Found), outcome.Message);

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Secret, Is.EqualTo(second));
        }

        [Test]
        public async Task NonTextBytesRoundTripUnchangedTest()
        {
            RequireWrite();
            string key = NewKey();

            byte[] secret = new byte[256];
            for (int i = 0; i < secret.Length; i++)
                secret[i] = (byte)i;

            await m_store.StoreAsync(key, secret);

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.Found), result.Message);
            Assert.That(result.Secret, Is.EqualTo(secret));
        }

        #endregion

        #region Absence Tests

        [Test]
        public async Task ReadOfAbsentKeyIsNotFoundTest()
        {
            SecretResult result = await m_store.ReadAsync(NewKey());

            Assert.That(result.Status, Is.EqualTo(SecretStatus.NotFound), result.Message);
            Assert.That(result.Secret, Is.Null);
        }

        [Test]
        public async Task DeleteOfAbsentKeySucceedsTest()
        {
            RequireWrite();
            SecretOutcome outcome = await m_store.DeleteAsync(NewKey());

            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.NotFound), outcome.Message);
        }

        [Test]
        public async Task DeleteThenReadIsNotFoundTest()
        {
            RequireWrite();
            string key = NewKey();

            await m_store.StoreAsync(key, RandomSecret(32));
            SecretOutcome outcome = await m_store.DeleteAsync(key);
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.NotFound), outcome.Message);

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.NotFound), result.Message);
        }

        #endregion

        #region Size Tests

        [Test]
        public async Task SecretAtMaximumSizeRoundTripsTest()
        {
            RequireWrite();
            string key = NewKey();
            byte[] secret = RandomSecret(SecretStoreBase.MAX_SECRET_SIZE);

            SecretOutcome outcome = await m_store.StoreAsync(key, secret);
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Found), outcome.Message);

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Secret, Is.EqualTo(secret));
        }

        [Test]
        public async Task SecretOneByteOverMaximumFailsWithLimitInMessageTest()
        {
            RequireWrite();
            string key = NewKey();
            byte[] secret = RandomSecret(SecretStoreBase.MAX_SECRET_SIZE + 1);

            SecretOutcome outcome = await m_store.StoreAsync(key, secret);

            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Failed));
            Assert.That(outcome.Message, Does.Contain($"{SecretStoreBase.MAX_SECRET_SIZE}"));

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.NotFound),
                "A refused store must not leave anything behind");
        }

        [Test]
        public async Task EmptySecretIsRefusedDistinctlyFromMissingTest()
        {
            RequireWrite();
            string key = NewKey();

            SecretOutcome outcome = await m_store.StoreAsync(key, ReadOnlyMemory<byte>.Empty);
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Failed),
                "\"\" and absent must never be confused");

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.NotFound));
        }

        #endregion

        #region Key Tests

        [Test]
        public void InvalidKeyThrowsTest()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await m_store.ReadAsync(null!));
            Assert.ThrowsAsync<ArgumentException>(async () => await m_store.ReadAsync(""));
            Assert.ThrowsAsync<ArgumentException>(async () => await m_store.ReadAsync("bad key with spaces"));
            Assert.ThrowsAsync<ArgumentException>(async () => await m_store.ReadAsync(new string('a', SecretKeys.MAX_LENGTH + 1)));
        }

        [Test]
        public async Task KeyAtLengthLimitDoesNotThrowTest()
        {
            RequireWrite();
            string prefix = NewKey() + "/";
            string key = prefix + new string('a', SecretKeys.MAX_LENGTH - prefix.Length);
            m_usedKeys.Add(key);

            SecretOutcome outcome = await m_store.StoreAsync(key, RandomSecret(16));
            Assert.That(outcome.Status, Is.EqualTo(SecretStatus.Found), outcome.Message);

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.Found), result.Message);
        }

        [Test]
        public async Task KeysDifferingOnlyInCaseAreTwoSecretsTest()
        {
            RequireWrite();
            string stem = NewKey();
            string lower = stem + "a";
            string upper = stem + "A";
            m_usedKeys.Add(lower);
            m_usedKeys.Add(upper);

            byte[] secretLower = RandomSecret(32);
            byte[] secretUpper = RandomSecret(32);

            await m_store.StoreAsync(lower, secretLower);
            await m_store.StoreAsync(upper, secretUpper);

            SecretResult resultLower = await m_store.ReadAsync(lower);
            SecretResult resultUpper = await m_store.ReadAsync(upper);

            Assert.That(resultLower.Secret, Is.EqualTo(secretLower),
                "The platform mapping is not injective: two keys collided into one credential");
            Assert.That(resultUpper.Secret, Is.EqualTo(secretUpper),
                "The platform mapping is not injective: two keys collided into one credential");
        }

        #endregion

        #region Concurrency Tests

        [Test]
        public async Task ConcurrentWritesToOneKeyLeaveOneWholeValueTest()
        {
            RequireWrite();
            string key = NewKey();

            const int writers = 12;
            byte[][] payloads = new byte[writers][];
            for (int i = 0; i < writers; i++)
            {
                payloads[i] = new byte[64 + (i * 8)];
                Array.Fill(payloads[i], (byte)(i + 1));
            }

            await Task.WhenAll(payloads.Select(payload =>
                Task.Run(() => m_store.StoreAsync(key, payload))));

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.Found), result.Message);
            Assert.That(payloads.Any(payload => payload.SequenceEqual(result.Secret!)),
                "The stored value is not any one of the written values — a torn or mixed write");
        }

        #endregion

        #region Check Tests

        [Test]
        public async Task CheckOnAReachableStoreSucceedsTest()
        {
            SecretOutcome outcome = await m_store.CheckAsync();

            Assert.That(outcome.IsSuccess(), Is.True,
                $"The store under test is expected reachable in its test environment; " +
                $"it answered {outcome.Status}: {outcome.Message}");
        }

        #endregion

        #region Description Tests

        [Test]
        public void DescriptionIsPopulatedTest()
        {
            SecretStoreDescription description = m_store.Description;

            Assert.That(description, Is.Not.Null);
            Assert.That(description.Key, Is.Not.Empty);
            Assert.That(description.Protection, Is.Not.EqualTo(SecretProtection.Unknown),
                "A host that cannot see what it got cannot refuse it");
            Assert.That(description.Location, Is.Not.Empty);
        }

        #endregion

        #region Tools

        /// <summary>
        /// A fresh key under the conformance namespace, tracked for cleanup.
        /// </summary>
        /// <returns>The key.</returns>
        protected string NewKey()
        {
            string key = $"OutWit.Conformance/{Guid.NewGuid():N}";
            m_usedKeys.Add(key);
            return key;
        }

        private void RequireWrite()
        {
            if (!m_store.Description.CanWrite)
                Assert.Ignore($"The '{m_store.Description.Key}' store is read-only; mutation tests are skipped.");
        }

        private static byte[] RandomSecret(int length)
        {
            return RandomNumberGenerator.GetBytes(length);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The store under test, fresh per test.
        /// </summary>
        protected ISecretStore Store => m_store;

        #endregion
    }
}
