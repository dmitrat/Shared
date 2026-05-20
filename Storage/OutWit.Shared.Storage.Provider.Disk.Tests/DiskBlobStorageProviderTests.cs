using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Shared.Storage.Provider.Disk;

namespace OutWit.Shared.Storage.Provider.Disk.Tests
{
    [TestFixture]
    public class DiskBlobStorageProviderTests
    {
        #region Fields

        private string m_testDir = null!;
        private DiskBlobStorageProvider m_provider = null!;

        #endregion

        #region Setup

        [SetUp]
        public void SetUp()
        {
            m_testDir = Path.Combine(Path.GetTempPath(), $"outwit_blob_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(m_testDir);

            var settings = new DiskBlobStorageSettings { StoragePath = m_testDir };
            m_provider = new DiskBlobStorageProvider(settings, NullLogger<DiskBlobStorageProvider>.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(m_testDir))
                Directory.Delete(m_testDir, recursive: true);
        }

        #endregion

        #region Write Tests

        [Test]
        public async Task WriteAndReadBlobTest()
        {
            var blobId = Guid.NewGuid();
            var data = "Hello, OutWit Blob Storage!"u8.ToArray();

            await using var writeStream = new MemoryStream(data);
            await m_provider.WriteAsync(blobId, "data.bin", writeStream);

            await using var readStream = await m_provider.ReadAsync(blobId, "data.bin");
            using var ms = new MemoryStream();
            await readStream.CopyToAsync(ms);

            Assert.That(ms.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task WriteOverwritesExistingBlobTest()
        {
            var blobId = Guid.NewGuid();
            var data1 = "First version"u8.ToArray();
            var data2 = "Second version"u8.ToArray();

            await using (var s1 = new MemoryStream(data1))
                await m_provider.WriteAsync(blobId, "data.bin", s1);

            await using (var s2 = new MemoryStream(data2))
                await m_provider.WriteAsync(blobId, "data.bin", s2);

            await using var readStream = await m_provider.ReadAsync(blobId, "data.bin");
            using var ms = new MemoryStream();
            await readStream.CopyToAsync(ms);

            Assert.That(ms.ToArray(), Is.EqualTo(data2));
        }

        [Test]
        public async Task WriteLargeBlobTest()
        {
            var blobId = Guid.NewGuid();
            var data = new byte[10 * 1024 * 1024]; // 10 MB
            Random.Shared.NextBytes(data);

            await using (var writeStream = new MemoryStream(data))
                await m_provider.WriteAsync(blobId, "data.bin", writeStream);

            var size = await m_provider.GetSizeAsync(blobId, "data.bin");
            Assert.That(size, Is.EqualTo(data.Length));
        }

        #endregion

        #region Append Tests

        [Test]
        public async Task AppendChunksTest()
        {
            var blobId = Guid.NewGuid();
            var chunk1 = "chunk-one-"u8.ToArray();
            var chunk2 = "chunk-two-"u8.ToArray();
            var chunk3 = "chunk-three"u8.ToArray();

            await m_provider.AppendAsync(blobId, "data.bin", chunk1);
            await m_provider.AppendAsync(blobId, "data.bin", chunk2);
            await m_provider.AppendAsync(blobId, "data.bin", chunk3);

            await using var readStream = await m_provider.ReadAsync(blobId, "data.bin");
            using var ms = new MemoryStream();
            await readStream.CopyToAsync(ms);

            var expected = "chunk-one-chunk-two-chunk-three"u8.ToArray();
            Assert.That(ms.ToArray(), Is.EqualTo(expected));
        }

        #endregion

        #region ReadChunk Tests

        [Test]
        public async Task ReadChunkReturnsCorrectRangeTest()
        {
            var blobId = Guid.NewGuid();
            var data = "0123456789ABCDEF"u8.ToArray();

            await using (var s = new MemoryStream(data))
                await m_provider.WriteAsync(blobId, "data.bin", s);

            var chunk = await m_provider.ReadChunkAsync(blobId, "data.bin", offset: 4, length: 6);
            Assert.That(chunk, Is.EqualTo("456789"u8.ToArray()));
        }

        [Test]
        public async Task ReadChunkAtEndReturnsShorterBufferTest()
        {
            var blobId = Guid.NewGuid();
            var data = "SHORT"u8.ToArray();

            await using (var s = new MemoryStream(data))
                await m_provider.WriteAsync(blobId, "data.bin", s);

            var chunk = await m_provider.ReadChunkAsync(blobId, "data.bin", offset: 3, length: 100);
            Assert.That(chunk, Is.EqualTo("RT"u8.ToArray()));
        }

        #endregion

        #region Delete Tests

        [Test]
        public async Task DeleteRemovesBlobDirectoryTest()
        {
            var blobId = Guid.NewGuid();

            await using (var s = new MemoryStream("data"u8.ToArray()))
                await m_provider.WriteAsync(blobId, "data.bin", s);

            Assert.That(await m_provider.ExistsAsync(blobId, "data.bin"), Is.True);

            await m_provider.DeleteAsync(blobId);

            Assert.That(await m_provider.ExistsAsync(blobId, "data.bin"), Is.False);
        }

        [Test]
        public async Task DeleteNonExistentBlobDoesNotThrowTest()
        {
            await m_provider.DeleteAsync(Guid.NewGuid());
        }

        #endregion

        #region Exists Tests

        [Test]
        public async Task ExistsReturnsFalseForMissingBlobTest()
        {
            Assert.That(await m_provider.ExistsAsync(Guid.NewGuid(), "data.bin"), Is.False);
        }

        [Test]
        public async Task ExistsReturnsTrueAfterWriteTest()
        {
            var blobId = Guid.NewGuid();

            await using (var s = new MemoryStream("data"u8.ToArray()))
                await m_provider.WriteAsync(blobId, "data.bin", s);

            Assert.That(await m_provider.ExistsAsync(blobId, "data.bin"), Is.True);
        }

        #endregion

        #region GetSize Tests

        [Test]
        public async Task GetSizeReturnsCorrectValueTest()
        {
            var blobId = Guid.NewGuid();
            var data = new byte[12345];
            Random.Shared.NextBytes(data);

            await using (var s = new MemoryStream(data))
                await m_provider.WriteAsync(blobId, "data.bin", s);

            var size = await m_provider.GetSizeAsync(blobId, "data.bin");
            Assert.That(size, Is.EqualTo(12345));
        }

        [Test]
        public void GetSizeThrowsForMissingBlobTest()
        {
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await m_provider.GetSizeAsync(Guid.NewGuid(), "data.bin"));
        }

        [Test]
        public void ReadMissingBlobThrowsTest()
        {
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await m_provider.ReadAsync(Guid.NewGuid(), "data.bin"));
        }

        #endregion

        #region Security Tests

        [Test]
        public void PathTraversalInFileNameIsRejectedTest()
        {
            var blobId = Guid.NewGuid();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await m_provider.WriteAsync(blobId, "../../../etc/passwd", new MemoryStream("x"u8.ToArray())));

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await m_provider.WriteAsync(blobId, "..\\..\\Windows\\system32\\config", new MemoryStream("x"u8.ToArray())));

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await m_provider.WriteAsync(blobId, "sub/folder/file.txt", new MemoryStream("x"u8.ToArray())));

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await m_provider.ReadAsync(blobId, "../../secret.txt"));
        }

        [Test]
        public async Task EmptyFileWriteAndReadTest()
        {
            var blobId = Guid.NewGuid();

            await using (var s = new MemoryStream(Array.Empty<byte>()))
                await m_provider.WriteAsync(blobId, "data.bin", s);

            Assert.That(await m_provider.ExistsAsync(blobId, "data.bin"), Is.True);
            Assert.That(await m_provider.GetSizeAsync(blobId, "data.bin"), Is.EqualTo(0));
        }

        #endregion

        #region Multiple Files Tests

        [Test]
        public async Task MultipleFilesPerBlobTest()
        {
            var blobId = Guid.NewGuid();

            await using (var s1 = new MemoryStream("archive-data"u8.ToArray()))
                await m_provider.WriteAsync(blobId, "archive.zip", s1);

            await using (var s2 = new MemoryStream("readme-text"u8.ToArray()))
                await m_provider.WriteAsync(blobId, "readme.md", s2);

            Assert.That(await m_provider.ExistsAsync(blobId, "archive.zip"), Is.True);
            Assert.That(await m_provider.ExistsAsync(blobId, "readme.md"), Is.True);

            var archiveSize = await m_provider.GetSizeAsync(blobId, "archive.zip");
            var readmeSize = await m_provider.GetSizeAsync(blobId, "readme.md");

            Assert.That(archiveSize, Is.EqualTo("archive-data"u8.ToArray().Length));
            Assert.That(readmeSize, Is.EqualTo("readme-text"u8.ToArray().Length));
        }

        #endregion
    }
}
