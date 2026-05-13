using OutWit.Common.Logging.Query.Model;
using OutWit.Shared.Logging.Provider.File;

namespace OutWit.Shared.Logging.Provider.File.Tests
{
    [TestFixture]
    public class LogEntryJsonParserTests
    {
        #region NewRelicFormatter shape

        [Test]
        public void ParsesNewRelicFormatterLineTest()
        {
            // NewRelic enricher writes Unix-ms timestamps and lowercase log.level.
            var line = "{\"timestamp\":1700000000000,\"log.level\":\"error\",\"message\":\"boom\",\"service.name\":\"WitIdentity\",\"hostname\":\"box-1\",\"logger.name\":\"Auth.Login\",\"error.stack\":\"at X()\"}";

            var entry = LogEntryJsonParser.TryParse(line);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Timestamp.Year, Is.GreaterThan(2020));
            Assert.That(entry.Level, Is.EqualTo(LogSeverity.Error));
            Assert.That(entry.Message, Is.EqualTo("boom"));
            Assert.That(entry.ServiceName, Is.EqualTo("WitIdentity"));
            Assert.That(entry.Host, Is.EqualTo("box-1"));
            Assert.That(entry.SourceContext, Is.EqualTo("Auth.Login"));
            Assert.That(entry.Exception, Is.EqualTo("at X()"));
        }

        #endregion

        #region Serilog compact JSON shape

        [Test]
        public void ParsesSerilogCompactJsonLineTest()
        {
            var line = "{\"@t\":\"2024-05-13T10:00:00Z\",\"@l\":\"Warning\",\"@mt\":\"User {Name} signed in\",\"Properties\":{\"SourceContext\":\"Auth.Login\"}}";

            var entry = LogEntryJsonParser.TryParse(line);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Level, Is.EqualTo(LogSeverity.Warning));
            Assert.That(entry.SourceContext, Is.EqualTo("Auth.Login"));
            Assert.That(entry.Message, Is.EqualTo("User {Name} signed in"));
        }

        [Test]
        public void OmitsLevelDefaultsToInformationTest()
        {
            // Serilog compact JSON drops @l for Information.
            var line = "{\"@t\":\"2024-05-13T10:00:00Z\",\"@mt\":\"hello\"}";

            var entry = LogEntryJsonParser.TryParse(line);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Level, Is.EqualTo(LogSeverity.Information));
        }

        #endregion

        #region Edge cases

        [Test]
        public void BlankLineReturnsNullTest()
        {
            Assert.That(LogEntryJsonParser.TryParse(""), Is.Null);
            Assert.That(LogEntryJsonParser.TryParse("   "), Is.Null);
            Assert.That(LogEntryJsonParser.TryParse(null!), Is.Null);
        }

        [Test]
        public void NonJsonLineReturnsNullTest()
        {
            Assert.That(LogEntryJsonParser.TryParse("not a json line"), Is.Null);
        }

        [Test]
        public void NonObjectJsonReturnsNullTest()
        {
            Assert.That(LogEntryJsonParser.TryParse("[1,2,3]"), Is.Null);
            Assert.That(LogEntryJsonParser.TryParse("\"string\""), Is.Null);
        }

        [Test]
        public void NoTimestampReturnsNullTest()
        {
            var line = "{\"@l\":\"Warning\",\"@mt\":\"no time\"}";
            Assert.That(LogEntryJsonParser.TryParse(line), Is.Null);
        }

        [TestCase("trace",       "Trace")]
        [TestCase("debug",       "Debug")]
        [TestCase("info",        "Information")]
        [TestCase("information", "Information")]
        [TestCase("warn",        "Warning")]
        [TestCase("warning",     "Warning")]
        [TestCase("error",       "Error")]
        [TestCase("critical",    "Critical")]
        [TestCase("fatal",       "Fatal")]
        public void LevelStringsAreNormalizedTest(string raw, string expected)
        {
            var line = "{\"@t\":\"2024-05-13T10:00:00Z\",\"@l\":\"" + raw + "\",\"@mt\":\"x\"}";

            var entry = LogEntryJsonParser.TryParse(line);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Level?.Value, Is.EqualTo(expected));
        }

        #endregion
    }
}
