using OutWit.Shared.Secrets.Conformance;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Providers.Tests
{
    [TestFixture]
    public class SecretStoreMemoryConformanceTests : SecretStoreConformanceTests
    {
        #region Initialization

        protected override ISecretStore CreateStore()
        {
            return new SecretStoreMemory();
        }

        #endregion
    }
}
