using System.Runtime.Versioning;
using OutWit.Shared.Secrets.Conformance;
using OutWit.Shared.Secrets.Provider.MacOS;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.MacOS.Tests
{
    /// <summary>
    /// Needs a signed-in macOS session with an unlocked login keychain. On a hosted macOS CI
    /// agent the login keychain is typically available; if items prompt, the run hangs — see
    /// the provider README on ACLs.
    /// </summary>
    [TestFixture]
    [Platform(Include = "MacOsX")]
    [SupportedOSPlatform("macos")]
    public class SecretStoreKeychainConformanceTests : SecretStoreConformanceTests
    {
        #region Initialization

        protected override ISecretStore CreateStore()
        {
            return new SecretStoreKeychain();
        }

        #endregion
    }
}
