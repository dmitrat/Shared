using System.Runtime.Versioning;
using OutWit.Shared.Secrets.Conformance;
using OutWit.Shared.Secrets.Provider.Windows;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.Windows.Tests
{
    [TestFixture]
    [Platform(Include = "Win")]
    [SupportedOSPlatform("windows")]
    public class SecretStoreWindowsConformanceTests : SecretStoreConformanceTests
    {
        #region Initialization

        protected override ISecretStore CreateStore()
        {
            return new SecretStoreWindows();
        }

        #endregion
    }
}
