using System.Runtime.Versioning;
using OutWit.Shared.Secrets.Conformance;
using OutWit.Shared.Secrets.Provider.Linux;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.Linux.Tests
{
    /// <summary>
    /// Needs an interactive desktop session with an unlocked keyring — a headless agent
    /// without D-Bus answers Unavailable to everything and the round-trip rows fail honestly.
    /// Run on a desktop Linux CI runner (or locally under a real session).
    /// </summary>
    [TestFixture]
    [Platform(Include = "Linux")]
    [SupportedOSPlatform("linux")]
    public class SecretStoreLibsecretConformanceTests : SecretStoreConformanceTests
    {
        #region Initialization

        protected override ISecretStore CreateStore()
        {
            return new SecretStoreLibsecret();
        }

        #endregion
    }
}
