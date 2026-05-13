using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Logging.NewRelic.Interfaces;
using OutWit.Common.Logging.Query;
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
    }
}
