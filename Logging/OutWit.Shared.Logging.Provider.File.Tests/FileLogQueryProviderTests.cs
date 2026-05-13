using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Logging.Query.Model;
using OutWit.Shared.Logging.Provider.File;

namespace OutWit.Shared.Logging.Provider.File.Tests
{
    [TestFixture]
    public class FileLogQueryProviderTests
    {
        #region Fields

        private string m_logsDir = null!;

        #endregion

        #region Setup

        [SetUp]
        public void Setup()
        {
            m_logsDir = Path.Combine(Path.GetTempPath(), "FileLogQueryProviderTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_logsDir);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(m_logsDir))
                    Directory.Delete(m_logsDir, recursive: true);
            }
            catch { /* best-effort cleanup */ }
        }

        #endregion

        #region QueryAsync — happy path

        [Test]
        public async Task ReturnsEntriesInDescendingTimestampOrderTest()
        {
            WriteFile("log-1.json",
                JsonLine("2024-05-13T10:00:00Z", "Information", "first"),
                JsonLine("2024-05-13T10:01:00Z", "Information", "second"),
                JsonLine("2024-05-13T10:02:00Z", "Information", "third"));

            var provider = NewProvider();
            var page = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                PageSize = 100
            });

            Assert.That(page.Items.Length, Is.EqualTo(3));
            Assert.That(page.Items[0].Message, Is.EqualTo("third"));
            Assert.That(page.Items[1].Message, Is.EqualTo("second"));
            Assert.That(page.Items[2].Message, Is.EqualTo("first"));
        }

        [Test]
        public async Task FiltersByTimeRangeTest()
        {
            WriteFile("log-1.json",
                JsonLine("2024-05-13T08:00:00Z", "Information", "before"),
                JsonLine("2024-05-13T10:00:00Z", "Information", "inside"),
                JsonLine("2024-05-13T12:00:00Z", "Information", "after"));

            var provider = NewProvider();
            var page = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                PageSize = 100
            });

            Assert.That(page.Items.Length, Is.EqualTo(1));
            Assert.That(page.Items[0].Message, Is.EqualTo("inside"));
        }

        [Test]
        public async Task FiltersByLevelTest()
        {
            WriteFile("log-1.json",
                JsonLine("2024-05-13T10:00:00Z", "Information", "info"),
                JsonLine("2024-05-13T10:01:00Z", "Error",       "err"),
                JsonLine("2024-05-13T10:02:00Z", "Warning",     "warn"));

            var provider = NewProvider();
            var page = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                Filters = new[] { LogFilters.LevelEquals(LogSeverity.Error) },
                PageSize = 100
            });

            Assert.That(page.Items.Length, Is.EqualTo(1));
            Assert.That(page.Items[0].Message, Is.EqualTo("err"));
        }

        [Test]
        public async Task FullTextSearchMatchesMessageTest()
        {
            WriteFile("log-1.json",
                JsonLine("2024-05-13T10:00:00Z", "Information", "User signed in"),
                JsonLine("2024-05-13T10:01:00Z", "Information", "Password reset"));

            var provider = NewProvider();
            var page = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                FullTextSearch = "password",
                PageSize = 100
            });

            Assert.That(page.Items.Length, Is.EqualTo(1));
            Assert.That(page.Items[0].Message, Does.Contain("Password"));
        }

        #endregion

        #region Paging

        [Test]
        public async Task PagingHonoursOffsetAndPageSizeTest()
        {
            using (var writer = new StreamWriter(Path.Combine(m_logsDir, "log-1.json")))
            {
                for (var i = 0; i < 25; i++)
                    writer.WriteLine(JsonLine($"2024-05-13T10:00:{i:D2}Z", "Information", $"line-{i:D2}"));
            }

            var provider = NewProvider();

            var first = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                PageSize = 10
            });
            Assert.That(first.Items.Length, Is.EqualTo(10));
            Assert.That(first.HasMore, Is.True);

            var second = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                PageSize = 10,
                Offset = 20
            });
            Assert.That(second.Items.Length, Is.EqualTo(5));
            Assert.That(second.HasMore, Is.False);
        }

        #endregion

        #region Statistics

        [Test]
        public async Task StatisticsCountsByLevelTest()
        {
            WriteFile("log-1.json",
                JsonLine("2024-05-13T10:00:00Z", "Information", "i1"),
                JsonLine("2024-05-13T10:01:00Z", "Information", "i2"),
                JsonLine("2024-05-13T10:02:00Z", "Warning",     "w"),
                JsonLine("2024-05-13T10:03:00Z", "Error",       "e1"),
                JsonLine("2024-05-13T10:04:00Z", "Critical",    "c"));

            var provider = NewProvider();
            var stats = await provider.GetStatisticsAsync(
                new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc));

            Assert.That(stats.TotalCount, Is.EqualTo(5));
            Assert.That(stats.InfoCount,    Is.EqualTo(2));
            Assert.That(stats.WarningCount, Is.EqualTo(1));
            Assert.That(stats.ErrorCount,   Is.EqualTo(2)); // Error + Critical
            Assert.That(stats.DebugCount,   Is.EqualTo(0));
        }

        #endregion

        #region DistinctValues

        [Test]
        public async Task DistinctValuesDedupesAcrossFilesTest()
        {
            WriteFile("log-1.json",
                JsonLineWithSource("2024-05-13T10:00:00Z", "Information", "x", "Auth.Login"),
                JsonLineWithSource("2024-05-13T10:01:00Z", "Information", "x", "Auth.Login"));
            WriteFile("log-2.json",
                JsonLineWithSource("2024-05-13T10:02:00Z", "Information", "x", "Users.Roles"));

            var provider = NewProvider();
            var values = await provider.GetDistinctValuesAsync(
                new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                LogAttribute.SourceContext);

            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values, Does.Contain("Auth.Login"));
            Assert.That(values, Does.Contain("Users.Roles"));
        }

        #endregion

        #region StorageInfo

        [Test]
        public async Task StorageInfoReportsPerFileSizesTest()
        {
            WriteFile("log-1.json", JsonLine("2024-05-13T10:00:00Z", "Information", "x"));
            WriteFile("log-2.json", JsonLine("2024-05-13T10:01:00Z", "Information", "y"));

            var provider = NewProvider();
            var info = await provider.GetStorageInfoAsync();

            Assert.That(info.UsedBytes, Is.Not.Null);
            Assert.That(info.UsedBytes!.Value, Is.GreaterThan(0));
            Assert.That(info.Breakdown, Is.Not.Null);
            Assert.That(info.Breakdown!.Count, Is.EqualTo(2));
            Assert.That(info.Breakdown.Keys, Does.Contain("log-1.json"));
            Assert.That(info.Breakdown.Keys, Does.Contain("log-2.json"));
        }

        [Test]
        public async Task StorageInfoReturnsNullBytesWhenDirectoryMissingTest()
        {
            var provider = new FileLogQueryProvider(
                Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N")),
                "log-*.json",
                new FileLogProviderOptions(),
                NullLogger<FileLogQueryProvider>.Instance);

            var info = await provider.GetStorageInfoAsync();

            Assert.That(info.UsedBytes, Is.Null);
            Assert.That(info.Breakdown, Is.Null);
        }

        #endregion

        #region Edge cases

        [Test]
        public async Task ReturnsEmptyWhenDirectoryMissingTest()
        {
            var provider = new FileLogQueryProvider(
                Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N")),
                "log-*.json",
                new FileLogProviderOptions(),
                NullLogger<FileLogQueryProvider>.Instance);

            var page = await provider.QueryAsync(new LogQuery
            {
                From = DateTime.UtcNow.AddHours(-1),
                To   = DateTime.UtcNow,
                PageSize = 10
            });

            Assert.That(page.Items, Is.Empty);
            Assert.That(page.HasMore, Is.False);
        }

        [Test]
        public async Task IgnoresMalformedLinesTest()
        {
            WriteFile("log-1.json",
                JsonLine("2024-05-13T10:00:00Z", "Information", "good"),
                "not-a-json-line",
                "",
                "{ invalid json",
                JsonLine("2024-05-13T10:01:00Z", "Information", "also good"));

            var provider = NewProvider();
            var page = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                PageSize = 100
            });

            Assert.That(page.Items.Length, Is.EqualTo(2));
            Assert.That(page.Items.Select(e => e.Message), Is.EquivalentTo(new[] { "good", "also good" }));
        }

        [Test]
        public async Task MaxScannedEntriesCapsWorkTest()
        {
            using (var writer = new StreamWriter(Path.Combine(m_logsDir, "log-1.json")))
            {
                for (var i = 0; i < 100; i++)
                    writer.WriteLine(JsonLine($"2024-05-13T10:00:{i:D2}Z", "Information", $"line-{i:D2}"));
            }

            var provider = new FileLogQueryProvider(
                m_logsDir,
                "log-*.json",
                new FileLogProviderOptions { MaxScannedEntries = 10 },
                NullLogger<FileLogQueryProvider>.Instance);

            var page = await provider.QueryAsync(new LogQuery
            {
                From = new DateTime(2024, 5, 13, 9, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2024, 5, 13, 11, 0, 0, DateTimeKind.Utc),
                PageSize = 100
            });

            // Only the first 10 scanned lines were considered.
            Assert.That(page.Items.Length, Is.LessThanOrEqualTo(10));
        }

        #endregion

        #region Tools

        private FileLogQueryProvider NewProvider() => new(
            m_logsDir,
            "log-*.json",
            new FileLogProviderOptions(),
            NullLogger<FileLogQueryProvider>.Instance);

        private void WriteFile(string name, params string[] lines)
        {
            System.IO.File.WriteAllLines(Path.Combine(m_logsDir, name), lines);
        }

        private static string JsonLine(string timestamp, string level, string message)
        {
            return $"{{\"@t\":\"{timestamp}\",\"@l\":\"{level}\",\"@mt\":\"{message}\"}}";
        }

        private static string JsonLineWithSource(string timestamp, string level, string message, string source)
        {
            return $"{{\"@t\":\"{timestamp}\",\"@l\":\"{level}\",\"@mt\":\"{message}\",\"Properties\":{{\"SourceContext\":\"{source}\"}}}}";
        }

        #endregion
    }
}
