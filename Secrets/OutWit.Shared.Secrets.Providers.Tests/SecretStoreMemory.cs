using System.Collections.Concurrent;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Providers.Tests
{
    /// <summary>
    /// In-memory store: exercises <see cref="SecretStoreBase"/> and proves the conformance
    /// suite against a known-trivial implementation. Test-only — nothing here persists.
    /// </summary>
    internal sealed class SecretStoreMemory : SecretStoreBase
    {
        #region Fields

        private readonly ConcurrentDictionary<string, byte[]> m_secrets = new();

        private readonly SecretStoreDescription m_description;

        #endregion

        #region Constructors

        public SecretStoreMemory(bool canWrite = true)
        {
            m_description = new SecretStoreDescription
            {
                Key = "Memory",
                Protection = SecretProtection.FileOnly,
                CanWrite = canWrite,
                Location = "process memory (test-only)"
            };
        }

        #endregion

        #region Functions

        protected override Task<SecretOutcome> DoStoreAsync(string key, byte[] secret,
            CancellationToken token)
        {
            // The base clears the buffer as soon as this returns — keep a copy.
            m_secrets[key] = (byte[])secret.Clone();
            return Task.FromResult(new SecretOutcome { Status = SecretStatus.Found });
        }

        protected override Task<SecretResult> DoReadAsync(string key, CancellationToken token)
        {
            if (!m_secrets.TryGetValue(key, out byte[]? secret))
                return Task.FromResult(new SecretResult { Status = SecretStatus.NotFound });

            return Task.FromResult(new SecretResult
            {
                Status = SecretStatus.Found,
                Secret = (byte[])secret.Clone()
            });
        }

        protected override Task<SecretOutcome> DoDeleteAsync(string key, CancellationToken token)
        {
            m_secrets.TryRemove(key, out _);
            return Task.FromResult(new SecretOutcome { Status = SecretStatus.NotFound });
        }

        #endregion

        #region Tools

        /// <summary>
        /// Seeds a secret bypassing the write gate — for read-only store tests.
        /// </summary>
        internal void Seed(string key, byte[] secret)
        {
            m_secrets[key] = secret;
        }

        #endregion

        #region Properties

        public override SecretStoreDescription Description => m_description;

        #endregion
    }
}
