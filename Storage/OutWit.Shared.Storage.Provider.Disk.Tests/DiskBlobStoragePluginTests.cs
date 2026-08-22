using Microsoft.Extensions.DependencyInjection;
using OutWit.Shared.Storage.Provider.Disk;
using OutWit.Shared.Storage.Provider.Disk.Utils;
using OutWit.Shared.Storage.Providers;

namespace OutWit.Shared.Storage.Provider.Disk.Tests
{
    [TestFixture]
    public class DiskBlobStoragePluginTests
    {
        #region Constants

        private const string STORAGE_PATH_VARIABLE = "DiskBlobStorage__StoragePath";

        #endregion

        #region Fields

        private string m_testDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void SetUp()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), $"outwit_blob_plugin_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(m_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testDir))
                Directory.Delete(m_testDir, recursive: true);
        }

        #endregion

        #region Plugin Tests

        [Test]
        public void PluginHasCorrectKeyTest()
        {
            var plugin = new DiskBlobStorageProviderPlugin();
            Assert.That(plugin.Key, Is.EqualTo("Disk"));
        }

        [Test]
        public void PluginRegistersProviderInDiTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddSingleton(new DiskBlobStorageSettings { StoragePath = m_testDir });
            services.AddSingleton<IBlobStorageProvider, DiskBlobStorageProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var provider = serviceProvider.GetService<IBlobStorageProvider>();

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<DiskBlobStorageProvider>());
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void ResolveSettingsDefaultsToBlobsFolderUnderBaseDirectoryTest()
        {
            using var scope = new EnvironmentVariableScope(STORAGE_PATH_VARIABLE, null);

            var settings = Extensions.ResolveSettings("Production");

            Assert.That(settings.StoragePath, Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "@Blobs")));
        }

        [Test]
        public void ResolveSettingsHonoursTheEnvironmentVariableTest()
        {
            // The documented docker contract: an absolute path on a mounted volume.
            using var scope = new EnvironmentVariableScope(STORAGE_PATH_VARIABLE, m_testDir);

            var settings = Extensions.ResolveSettings("Production");

            Assert.That(settings.StoragePath, Is.EqualTo(m_testDir));
        }

        [Test]
        public void ResolveSettingsRootsARelativeEnvironmentValueUnderBaseDirectoryTest()
        {
            using var scope = new EnvironmentVariableScope(STORAGE_PATH_VARIABLE, "data/blobs");

            var settings = Extensions.ResolveSettings("Production");

            Assert.That(settings.StoragePath, Is.EqualTo(Path.Combine(AppContext.BaseDirectory, "data/blobs")));
        }

        [Test]
        public async Task AddDiskBlobStorageRegistersTheResolvedPathTest()
        {
            using var scope = new EnvironmentVariableScope(STORAGE_PATH_VARIABLE, m_testDir);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDiskBlobStorage("Production");

            var serviceProvider = services.BuildServiceProvider();
            var settings = serviceProvider.GetRequiredService<DiskBlobStorageSettings>();
            var provider = serviceProvider.GetRequiredService<IBlobStorageProvider>();

            Assert.That(settings.StoragePath, Is.EqualTo(m_testDir));
            Assert.That(provider, Is.InstanceOf<DiskBlobStorageProvider>());

            // The provider writes where the variable points, not under the host binary.
            var blobId = Guid.NewGuid();
            await using (var stream = new MemoryStream("env"u8.ToArray()))
                await provider.WriteAsync(blobId, "data.bin", stream);

            Assert.That(File.Exists(Path.Combine(m_testDir, blobId.ToString("N"), "data.bin")), Is.True);
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task PluginRegisteredProviderWorksEndToEndTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(new DiskBlobStorageSettings { StoragePath = m_testDir });
            services.AddSingleton<IBlobStorageProvider, DiskBlobStorageProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var provider = serviceProvider.GetRequiredService<IBlobStorageProvider>();

            // Upload
            var blobId = Guid.NewGuid();
            var data = "Integration test data for OutWit blob storage"u8.ToArray();

            await using (var stream = new MemoryStream(data))
                await provider.WriteAsync(blobId, "data.bin", stream);

            // Verify exists
            Assert.That(await provider.ExistsAsync(blobId, "data.bin"), Is.True);

            // Download and verify
            byte[] readData;
            await using (var readStream = await provider.ReadAsync(blobId, "data.bin"))
            {
                using var ms = new MemoryStream();
                await readStream.CopyToAsync(ms);
                readData = ms.ToArray();
            }
            Assert.That(readData, Is.EqualTo(data));

            // Verify size
            var size = await provider.GetSizeAsync(blobId, "data.bin");
            Assert.That(size, Is.EqualTo(data.Length));

            // Cleanup
            await provider.DeleteAsync(blobId);
            Assert.That(await provider.ExistsAsync(blobId, "data.bin"), Is.False);
        }

        [Test]
        public async Task ChunkedUploadEndToEndTest()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(new DiskBlobStorageSettings { StoragePath = m_testDir });
            services.AddSingleton<IBlobStorageProvider, DiskBlobStorageProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var provider = serviceProvider.GetRequiredService<IBlobStorageProvider>();

            var blobId = Guid.NewGuid();

            var chunk1 = new byte[1024];
            var chunk2 = new byte[1024];
            var chunk3 = new byte[512];
            Random.Shared.NextBytes(chunk1);
            Random.Shared.NextBytes(chunk2);
            Random.Shared.NextBytes(chunk3);

            await provider.AppendAsync(blobId, "data.bin", chunk1);
            await provider.AppendAsync(blobId, "data.bin", chunk2);
            await provider.AppendAsync(blobId, "data.bin", chunk3);

            var size = await provider.GetSizeAsync(blobId, "data.bin");
            Assert.That(size, Is.EqualTo(1024 + 1024 + 512));

            var readChunk1 = await provider.ReadChunkAsync(blobId, "data.bin", 0, 1024);
            Assert.That(readChunk1, Is.EqualTo(chunk1));

            var readChunk2 = await provider.ReadChunkAsync(blobId, "data.bin", 1024, 1024);
            Assert.That(readChunk2, Is.EqualTo(chunk2));

            var readChunk3 = await provider.ReadChunkAsync(blobId, "data.bin", 2048, 512);
            Assert.That(readChunk3, Is.EqualTo(chunk3));
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Sets a process environment variable for the test and restores the previous value.
        /// </summary>
        private sealed class EnvironmentVariableScope : IDisposable
        {
            private readonly string m_name;
            private readonly string? m_previous;

            public EnvironmentVariableScope(string name, string? value)
            {
                m_name = name;
                m_previous = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(m_name, m_previous);
            }
        }

        #endregion
    }
}
