using System.Runtime.Versioning;
using OutWit.Shared.Secrets.Provider.Linux;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.Linux.Tests
{
    [TestFixture]
    [Platform(Include = "Linux")]
    [SupportedOSPlatform("linux")]
    public class SecretStoreLibsecretTests
    {
        #region Availability Tests

        [Test]
        [Explicit("Run in an environment with no session bus (unset DBUS_SESSION_BUS_ADDRESS " +
                  "in a fresh process, or a headless agent): every operation must answer " +
                  "Unavailable — not an exception, and never NotFound.")]
        public async Task NoSessionBusIsUnavailableTest()
        {
            var store = new SecretStoreLibsecret();

            SecretResult result = await store.ReadAsync("OutWit.Tests/NoBus");

            Assert.That(result.Status, Is.EqualTo(SecretStatus.Unavailable),
                "With no Secret Service the honest answer is Unavailable; " +
                "NotFound here becomes a false \"not provisioned\"");
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void DescriptionSaysHowToInspectTest()
        {
            var store = new SecretStoreLibsecret();

            Assert.That(store.Description.Key, Is.EqualTo("Libsecret"));
            Assert.That(store.Description.Location, Does.Contain("secret-tool"));
        }

        #endregion
    }
}
