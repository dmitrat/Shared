using OutWit.Shared.Secrets.Desktop;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Desktop.Tests
{
    [TestFixture]
    public class SecretStoreDesktopTests
    {
        #region Factory Tests

        [Test]
        public void ForCurrentPlatformReturnsTheOsStoreTest()
        {
            ISecretStore store = SecretStoreDesktop.ForCurrentPlatform();

            string expected = OperatingSystem.IsWindows() ? "Windows"
                : OperatingSystem.IsMacOS() ? "Keychain"
                : OperatingSystem.IsLinux() ? "Libsecret"
                : throw new PlatformNotSupportedException();

            Assert.That(store.Description.Key, Is.EqualTo(expected));
            Assert.That(store.Description.Protection,
                Is.EqualTo(SecretProtection.OperatingSystem),
                "The factory must only ever hand out an OS store — degradation is not automatic");
        }

        #endregion
    }
}
