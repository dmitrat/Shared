using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Providers.Tests
{
    /// <summary>
    /// A deliberately misbehaving provider: answers and throws whatever a test tells it to,
    /// so the base class's normalizations and guards can be proven against exactly the
    /// mistakes a real provider might make.
    /// </summary>
    internal sealed class SecretStoreMisbehaving : SecretStoreBase
    {
        #region Fields

        private readonly SecretStoreDescription m_description = new SecretStoreDescription
        {
            Key = "Misbehaving",
            Protection = SecretProtection.FileOnly,
            CanWrite = true,
            Location = "nowhere (test-only)"
        };

        #endregion

        #region Functions

        protected override Task<SecretOutcome> DoStoreAsync(string key, byte[] secret,
            CancellationToken token)
        {
            LastStoreBuffer = secret;

            if (ThrowOnStore != null)
                throw ThrowOnStore;

            return Task.FromResult(StoreAnswer ?? new SecretOutcome { Status = SecretStatus.Found });
        }

        protected override Task<SecretResult> DoReadAsync(string key, CancellationToken token)
        {
            if (ThrowOnRead != null)
                throw ThrowOnRead;

            return Task.FromResult(ReadAnswer ?? new SecretResult { Status = SecretStatus.NotFound });
        }

        protected override Task<SecretOutcome> DoDeleteAsync(string key, CancellationToken token)
        {
            return Task.FromResult(new SecretOutcome { Status = SecretStatus.NotFound });
        }

        protected override (SecretStatus Status, string Message)? MapException(Exception exception)
        {
            if (exception is TimeoutException)
                return (SecretStatus.Unavailable, "mapped: " + exception.Message);

            return null;
        }

        #endregion

        #region Properties

        internal SecretOutcome? StoreAnswer { get; set; }

        internal SecretResult? ReadAnswer { get; set; }

        internal Exception? ThrowOnStore { get; set; }

        internal Exception? ThrowOnRead { get; set; }

        internal byte[]? LastStoreBuffer { get; private set; }

        public override SecretStoreDescription Description => m_description;

        #endregion
    }
}
