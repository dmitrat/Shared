using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Logging.NewRelic.Interfaces;
using OutWit.Common.Logging.NewRelic.Model;
using OutWit.Common.Logging.Query;
using OutWit.Common.Logging.Query.Model;
using OutWit.Shared.Logging.Provider.NewRelic;
using OutWit.Shared.Logging.Providers;

namespace OutWit.Shared.Logging.Provider.NewRelic.Tests
{
    [TestFixture]
    public class NewRelicLogProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new NewRelicLogProviderPlugin();
            Assert.That(plugin.Key, Is.EqualTo(NewRelicLogProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("NewRelic"));
        }

        [Test]
        public void PluginImplementsILogProviderPluginTest()
        {
            var plugin = new NewRelicLogProviderPlugin();
            Assert.That(plugin, Is.InstanceOf<ILogProviderPlugin>());
        }

        #endregion

        #region Initialize

        [Test]
        public void InitializeRegistersILogQueryProviderTest()
        {
            Environment.SetEnvironmentVariable("NewRelic__ApiKey", "NRAK-test-key");
            Environment.SetEnvironmentVariable("NewRelic__AccountId", "1234567");
            try
            {
                var plugin = new NewRelicLogProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var provider = sp.GetService<ILogQueryProvider>();
                var nrProvider = sp.GetService<INewRelicProvider>();

                Assert.That(provider, Is.Not.Null);
                Assert.That(nrProvider, Is.Not.Null);
                Assert.That(provider, Is.SameAs(nrProvider), "ILogQueryProvider must resolve to the same singleton as INewRelicProvider.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("NewRelic__ApiKey", null);
                Environment.SetEnvironmentVariable("NewRelic__AccountId", null);
            }
        }

        [Test]
        public void InitializeThrowsWhenApiKeyMissingTest()
        {
            // No env vars set — appsettings.json ships with blank ApiKey.
            var plugin = new NewRelicLogProviderPlugin();
            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => plugin.Initialize(services));
        }

        [Test]
        public void InitializeThrowsWhenAccountIdMissingTest()
        {
            Environment.SetEnvironmentVariable("NewRelic__ApiKey", "NRAK-test-key");
            try
            {
                var plugin = new NewRelicLogProviderPlugin();
                var services = new ServiceCollection();

                Assert.Throws<InvalidOperationException>(() => plugin.Initialize(services));
            }
            finally
            {
                Environment.SetEnvironmentVariable("NewRelic__ApiKey", null);
            }
        }

        #endregion

        #region BaseFilters

        [Test]
        public void BaseFiltersDefaultIsEmptyWhenUnsetTest()
        {
            // Without any BaseFilters env vars the plugin still works — empty
            // array = old behaviour (no NRQL scoping).
            Environment.SetEnvironmentVariable("NewRelic__ApiKey", "NRAK-test-key");
            Environment.SetEnvironmentVariable("NewRelic__AccountId", "1234567");
            try
            {
                var plugin = new NewRelicLogProviderPlugin();
                var services = new ServiceCollection();
                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var options = sp.GetRequiredService<NewRelicClientOptions>();

                Assert.That(options.BaseFilters, Is.Not.Null);
                Assert.That(options.BaseFilters, Is.Empty);
            }
            finally
            {
                Environment.SetEnvironmentVariable("NewRelic__ApiKey", null);
                Environment.SetEnvironmentVariable("NewRelic__AccountId", null);
            }
        }

        [Test]
        public void BaseFiltersBindFromEnvironmentVariablesTest()
        {
            // The whole point of BaseFilters — operators wire it up through
            // double-underscore env vars in docker-compose. Verify the
            // configuration binder picks them up as an array of LogFilter.
            Environment.SetEnvironmentVariable("NewRelic__ApiKey", "NRAK-test-key");
            Environment.SetEnvironmentVariable("NewRelic__AccountId", "1234567");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Attribute", "service.name");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Operator", "Equals");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Values__0", "WitIdentity");
            try
            {
                var plugin = new NewRelicLogProviderPlugin();
                var services = new ServiceCollection();
                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var options = sp.GetRequiredService<NewRelicClientOptions>();

                Assert.That(options.BaseFilters, Has.Length.EqualTo(1));
                Assert.That(options.BaseFilters[0].Attribute, Is.EqualTo("service.name"));
                Assert.That(options.BaseFilters[0].Operator, Is.EqualTo(LogFilterOperator.Equals));
                Assert.That(options.BaseFilters[0].Values, Is.EqualTo(new[] { "WitIdentity" }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("NewRelic__ApiKey", null);
                Environment.SetEnvironmentVariable("NewRelic__AccountId", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Attribute", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Operator", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Values__0", null);
            }
        }

        [Test]
        public void MultipleBaseFiltersBindFromEnvironmentVariablesTest()
        {
            // Composite filter — service + host. Documented in
            // PRODUCTION_LESSONS.md B.4 as a valid scoping shape.
            Environment.SetEnvironmentVariable("NewRelic__ApiKey", "NRAK-test-key");
            Environment.SetEnvironmentVariable("NewRelic__AccountId", "1234567");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Attribute", "service.name");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Operator", "Equals");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Values__0", "WitIdentity");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Attribute", "host");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Operator", "In");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Values__0", "auth-1");
            Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Values__1", "auth-2");
            try
            {
                var plugin = new NewRelicLogProviderPlugin();
                var services = new ServiceCollection();
                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var options = sp.GetRequiredService<NewRelicClientOptions>();

                Assert.That(options.BaseFilters, Has.Length.EqualTo(2));

                Assert.That(options.BaseFilters[0].Attribute, Is.EqualTo("service.name"));
                Assert.That(options.BaseFilters[0].Operator, Is.EqualTo(LogFilterOperator.Equals));
                Assert.That(options.BaseFilters[0].Values, Is.EqualTo(new[] { "WitIdentity" }));

                Assert.That(options.BaseFilters[1].Attribute, Is.EqualTo("host"));
                Assert.That(options.BaseFilters[1].Operator, Is.EqualTo(LogFilterOperator.In));
                Assert.That(options.BaseFilters[1].Values, Is.EqualTo(new[] { "auth-1", "auth-2" }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("NewRelic__ApiKey", null);
                Environment.SetEnvironmentVariable("NewRelic__AccountId", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Attribute", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Operator", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__0__Values__0", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Attribute", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Operator", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Values__0", null);
                Environment.SetEnvironmentVariable("NewRelic__BaseFilters__1__Values__1", null);
            }
        }

        #endregion
    }
}
