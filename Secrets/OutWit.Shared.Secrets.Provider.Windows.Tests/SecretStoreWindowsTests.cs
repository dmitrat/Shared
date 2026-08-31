using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using OutWit.Shared.Secrets.Provider.Windows;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.Windows.Tests
{
    [TestFixture]
    [Platform(Include = "Win")]
    [SupportedOSPlatform("windows")]
    public class SecretStoreWindowsTests
    {
        #region Mapping Tests

        [Test]
        public void MapKeyIsDeterministicAndSuffixedTest()
        {
            string first = SecretStoreWindows.MapKey("Test.Product/Purpose");
            string second = SecretStoreWindows.MapKey("Test.Product/Purpose");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Does.StartWith("Test.Product/Purpose#"));
            Assert.That(first, Does.Match(@"#[0-9a-f]{8}$"));
        }

        [Test]
        public void MapKeyIsInjectiveForCaseTest()
        {
            string lower = SecretStoreWindows.MapKey("Test.Product/purpose");
            string upper = SecretStoreWindows.MapKey("Test.Product/Purpose");

            Assert.That(string.Equals(lower, upper, StringComparison.OrdinalIgnoreCase), Is.False,
                "Target names are case-insensitive on Windows; the suffix must differ");
        }

        #endregion

        #region Vault Tests

        [Test]
        public async Task EntryAppearsUnderMappedTargetNameTest()
        {
            var store = new SecretStoreWindows();
            string key = $"OutWit.Tests/{Guid.NewGuid():N}";

            try
            {
                await store.StoreAsync(key, new byte[] { 1, 2, 3, 4 });

                string target = SecretStoreWindows.MapKey(key);
                bool found = CredReadW(target, 1, 0, out IntPtr credential);

                if (found)
                    CredFree(credential);

                Assert.That(found, Is.True,
                    $"The entry was not found under the documented target name '{target}'");
            }
            finally
            {
                await store.DeleteAsync(key);
            }
        }

        [Test]
        [Explicit("Needs a second account — the single most valuable test in the set. " +
                  "Arrange for it on a self-hosted agent: as another user, store a secret and " +
                  "set WIT_SECRETS_CROSSACCOUNT_TARGET to its mapped target name, then run this " +
                  "test to prove this account cannot read it.")]
        public void DifferentUserCannotReadTest()
        {
            string? target = Environment.GetEnvironmentVariable("WIT_SECRETS_CROSSACCOUNT_TARGET");

            if (string.IsNullOrEmpty(target))
                Assert.Ignore("WIT_SECRETS_CROSSACCOUNT_TARGET is not set; see the Explicit reason.");

            bool found = CredReadW(target, 1, 0, out IntPtr credential);

            if (found)
                CredFree(credential);

            Assert.That(found, Is.False,
                "A credential stored by another account is readable from this one — " +
                "the containment §3.2 promises does not hold");
        }

        #endregion

        #region Tools

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", EntryPoint = "CredFree")]
        private static extern void CredFree(IntPtr buffer);

        #endregion
    }
}
