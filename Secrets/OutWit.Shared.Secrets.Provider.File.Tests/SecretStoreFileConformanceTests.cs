using OutWit.Shared.Secrets.Conformance;
using OutWit.Shared.Secrets.Provider.File;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Provider.File.Tests
{
    [TestFixture]
    public class SecretStoreFileConformanceTests : SecretStoreConformanceTests
    {
        #region Fields

        private string m_directory = null!;

        #endregion

        #region Initialization

        protected override ISecretStore CreateStore()
        {
            m_directory = Path.Combine(Path.GetTempPath(), "OutWit.Secrets.Tests",
                Guid.NewGuid().ToString("N"));

            return new SecretStoreFile(new SecretStoreFileOptions
            {
                DirectoryPath = m_directory
            });
        }

        [TearDown]
        public void TeardownDirectory()
        {
            if (Directory.Exists(m_directory))
                Directory.Delete(m_directory, true);
        }

        #endregion
    }
}
