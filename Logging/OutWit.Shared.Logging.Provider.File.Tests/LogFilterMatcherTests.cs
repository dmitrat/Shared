using System;
using OutWit.Common.Logging.Query.Model;
using OutWit.Shared.Logging.Provider.File;

namespace OutWit.Shared.Logging.Provider.File.Tests
{
    [TestFixture]
    public class LogFilterMatcherTests
    {
        #region Setup

        private static LogEntry NewEntry(LogSeverity level, string message = "hello", string? source = "Auth")
        {
            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                SourceContext = source
            };
        }

        #endregion

        #region Equals / NotEquals

        [Test]
        public void LevelEqualsMatchesTest()
        {
            var entry = NewEntry(LogSeverity.Error);
            var filter = LogFilter.Eq(LogAttribute.Level, "Error");

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.True);
        }

        [Test]
        public void LevelEqualsRejectsTest()
        {
            var entry = NewEntry(LogSeverity.Warning);
            var filter = LogFilter.Eq(LogAttribute.Level, "Error");

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.False);
        }

        [Test]
        public void NotEqualsInvertsTest()
        {
            var entry = NewEntry(LogSeverity.Warning);
            var filter = LogFilter.NotEq(LogAttribute.Level, "Error");

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.True);
        }

        #endregion

        #region Contains / NotContains

        [Test]
        public void MessageContainsMatchesTest()
        {
            var entry = NewEntry(LogSeverity.Information, "User logged in");
            var filter = LogFilters.MessageContains("logged");

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.True);
        }

        [Test]
        public void MessageNotContainsMatchesTest()
        {
            var entry = NewEntry(LogSeverity.Information, "User logged in");
            var filter = LogFilters.MessageNotContains("forbidden");

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.True);
        }

        #endregion

        #region In

        [Test]
        public void LevelInMatchesAnyTest()
        {
            var entry = NewEntry(LogSeverity.Critical);
            var filter = LogFilters.LevelIn(LogSeverity.Error, LogSeverity.Critical, LogSeverity.Fatal);

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.True);
        }

        [Test]
        public void LevelInRejectsWhenNoneTest()
        {
            var entry = NewEntry(LogSeverity.Warning);
            var filter = LogFilters.LevelIn(LogSeverity.Error, LogSeverity.Critical);

            Assert.That(LogFilterMatcher.Matches(entry, new[] { filter }), Is.False);
        }

        #endregion

        #region Multiple filters (AND)

        [Test]
        public void MultipleFiltersAreAndedTest()
        {
            var entry = NewEntry(LogSeverity.Error, "boom", "Auth.Login");
            var filters = new[]
            {
                LogFilters.LevelEquals(LogSeverity.Error),
                LogFilters.SourceContextEquals("Auth.Login")
            };

            Assert.That(LogFilterMatcher.Matches(entry, filters), Is.True);
        }

        [Test]
        public void MultipleFiltersFailWhenOneRejectsTest()
        {
            var entry = NewEntry(LogSeverity.Error, "boom", "Auth.Login");
            var filters = new[]
            {
                LogFilters.LevelEquals(LogSeverity.Warning),  // mismatch
                LogFilters.SourceContextEquals("Auth.Login")
            };

            Assert.That(LogFilterMatcher.Matches(entry, filters), Is.False);
        }

        #endregion

        #region Free-text

        [Test]
        public void FreeTextSearchesMessageAndExceptionTest()
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogSeverity.Error,
                Message = "Operation failed",
                Exception = "at SignInAsync()"
            };

            Assert.That(LogFilterMatcher.MatchesFreeText(entry, "SignIn"), Is.True);
            Assert.That(LogFilterMatcher.MatchesFreeText(entry, "operation"), Is.True); // case-insensitive
            Assert.That(LogFilterMatcher.MatchesFreeText(entry, "passkey"), Is.False);
        }

        [Test]
        public void EmptyFreeTextMatchesEverythingTest()
        {
            var entry = NewEntry(LogSeverity.Information);
            Assert.That(LogFilterMatcher.MatchesFreeText(entry, null), Is.True);
            Assert.That(LogFilterMatcher.MatchesFreeText(entry, ""), Is.True);
            Assert.That(LogFilterMatcher.MatchesFreeText(entry, "   "), Is.True);
        }

        #endregion

        #region Empty filters

        [Test]
        public void NullOrEmptyFiltersMatchEverythingTest()
        {
            var entry = NewEntry(LogSeverity.Information);
            Assert.That(LogFilterMatcher.Matches(entry, null), Is.True);
            Assert.That(LogFilterMatcher.Matches(entry, System.Array.Empty<LogFilter>()), Is.True);
        }

        #endregion
    }
}
