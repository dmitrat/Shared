using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using IOFile = System.IO.File;
using OutWit.Shared.Secrets.Provider.File;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.File.Tests
{
    [TestFixture]
    public class SecretStoreFileTests
    {
        #region Fields

        private string m_directory = null!;

        private SecretStoreFile m_store = null!;

        #endregion

        #region Initialization

        [SetUp]
        public void Setup()
        {
            m_directory = Path.Combine(Path.GetTempPath(), "OutWit.Secrets.Tests",
                Guid.NewGuid().ToString("N"));
            m_store = new SecretStoreFile(new SecretStoreFileOptions
            {
                DirectoryPath = m_directory
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_directory))
                Directory.Delete(m_directory, true);
        }

        #endregion

        #region Permission Tests

        [Test]
        public async Task CreatedFilePermissionsAreOwnerOnlyTest()
        {
            string key = "Test/Permissions";
            await m_store.StoreAsync(key, new byte[] { 1, 2, 3 });

            string path = Path.Combine(m_directory, SecretStoreFile.MapFileName(key));
            Assert.That(IOFile.Exists(path), Is.True);

            if (OperatingSystem.IsWindows())
            {
                FileSecurity security = new FileInfo(path).GetAccessControl();
                Assert.That(security.AreAccessRulesProtected, Is.True,
                    "The file inherits permissions from its directory");

                SecurityIdentifier? user = WindowsIdentity.GetCurrent().User;
                foreach (FileSystemAccessRule rule in
                         security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    Assert.That(rule.IdentityReference, Is.EqualTo(user),
                        "The DACL names an identity other than the owning account");
                }
            }
            else
            {
                UnixFileMode mode = IOFile.GetUnixFileMode(path);
                Assert.That(mode, Is.EqualTo(UnixFileMode.UserRead | UnixFileMode.UserWrite));
            }
        }

        #endregion

        #region Crash Tests

        [Test]
        public async Task CrashBetweenWriteAndRenameLeavesPreviousValueTest()
        {
            string key = "Test/Crash";
            byte[] previous = Encoding.UTF8.GetBytes("previous-value");

            await m_store.StoreAsync(key, previous);

            // Simulate a writer that died between write and rename: a leftover temp file.
            string path = Path.Combine(m_directory, SecretStoreFile.MapFileName(key));
            string stale = $"{path}.{Guid.NewGuid():N}.tmp";
            await IOFile.WriteAllBytesAsync(stale, Encoding.UTF8.GetBytes("torn half-write"));

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Status, Is.EqualTo(SecretStatus.Found), result.Message);
            Assert.That(result.Secret, Is.EqualTo(previous),
                "A leftover temp file must never shadow the committed value");

            // The next successful store sweeps the stale temp.
            await m_store.StoreAsync(key, Encoding.UTF8.GetBytes("next-value"));
            Assert.That(IOFile.Exists(stale), Is.False, "Stale temp files must be swept");
        }

        #endregion

        #region Protection Tests

        [Test]
        public async Task PlatformKeyProtectionHidesPlaintextOnDiskTest()
        {
            if (!OperatingSystem.IsWindows())
                Assert.Ignore("DPAPI protection is Windows-only; elsewhere the store is FileOnly.");

            Assert.That(m_store.Description.Protection,
                Is.EqualTo(SecretProtection.FileWithPlatformKey));

            string key = "Test/Dpapi";
            byte[] plaintext = Encoding.UTF8.GetBytes("FINDABLE-PLAINTEXT-MARKER");
            await m_store.StoreAsync(key, plaintext);

            string path = Path.Combine(m_directory, SecretStoreFile.MapFileName(key));
            byte[] onDisk = await IOFile.ReadAllBytesAsync(path);

            Assert.That(Encoding.UTF8.GetString(onDisk), Does.Not.Contain("FINDABLE-PLAINTEXT-MARKER"),
                "The payload sits in plaintext although a platform key was promised");

            SecretResult result = await m_store.ReadAsync(key);
            Assert.That(result.Secret, Is.EqualTo(plaintext));
        }

        [Test]
        public async Task FileOnlyWhenPlatformKeyDisabledTest()
        {
            var store = new SecretStoreFile(new SecretStoreFileOptions
            {
                DirectoryPath = m_directory,
                UsePlatformKey = false
            });

            Assert.That(store.Description.Protection, Is.EqualTo(SecretProtection.FileOnly));

            string key = "Test/FileOnly";
            byte[] plaintext = Encoding.UTF8.GetBytes("plain-value");
            await store.StoreAsync(key, plaintext);

            SecretResult result = await store.ReadAsync(key);
            Assert.That(result.Secret, Is.EqualTo(plaintext));
        }

        #endregion

        #region Corruption Tests

        [Test]
        public async Task CorruptFileIsFailedNotNotFoundTest()
        {
            string key = "Test/Corrupt";
            string path = Path.Combine(m_directory, SecretStoreFile.MapFileName(key));

            Directory.CreateDirectory(m_directory);
            await IOFile.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes("not an envelope"));

            SecretResult result = await m_store.ReadAsync(key);

            Assert.That(result.Status, Is.EqualTo(SecretStatus.Failed),
                "Corrupt must never read as \"no secret\"");
            Assert.That(result.Message, Does.Contain(path));
        }

        [Test]
        public async Task NewerFormatVersionIsFailedWithVersionsNamedTest()
        {
            string key = "Test/Newer";
            string path = Path.Combine(m_directory, SecretStoreFile.MapFileName(key));

            Directory.CreateDirectory(m_directory);
            byte[] envelope = { (byte)'W', (byte)'S', (byte)'E', (byte)'C', 99, 0, 0, 0, 1, 2, 3 };
            await IOFile.WriteAllBytesAsync(path, envelope);

            SecretResult result = await m_store.ReadAsync(key);

            Assert.That(result.Status, Is.EqualTo(SecretStatus.Failed));
            Assert.That(result.Message, Does.Contain("99"));
            Assert.That(result.Message, Does.Contain("newer"));
        }

        #endregion

        #region Mapping Tests

        [Test]
        public void MapFileNameIsInjectiveForCaseTest()
        {
            string lower = SecretStoreFile.MapFileName("Test/purpose");
            string upper = SecretStoreFile.MapFileName("Test/Purpose");

            Assert.That(string.Equals(lower, upper, StringComparison.OrdinalIgnoreCase), Is.False,
                "File systems may be case-insensitive; the hash suffix must differ");
        }

        [Test]
        public void MapFileNameFlattensSlashesTest()
        {
            string name = SecretStoreFile.MapFileName("Product/Sub/Purpose");

            Assert.That(name, Does.Not.Contain('/'));
            Assert.That(name, Does.EndWith(".wsecret"));
        }

        #endregion
    }
}
