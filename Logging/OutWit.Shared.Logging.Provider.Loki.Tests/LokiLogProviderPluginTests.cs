using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Logging.Loki;
using OutWit.Common.Logging.Query;
using OutWit.Shared.Logging.Provider.Loki;
using OutWit.Shared.Logging.Providers;

namespace OutWit.Shared.Logging.Provider.Loki.Tests
{
    [TestFixture]
    public class LokiLogProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new LokiLogProviderPlugin();
            Assert.That(plugin.Key, Is.EqualTo(LokiLogProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("Loki"));
        }

        [Test]
        public void PluginImplementsILogProviderPluginTest()
        {
            var plugin = new LokiLogProviderPlugin();
            Assert.That(plugin, Is.InstanceOf<ILogProviderPlugin>());
        }

        #endregion

        #region Initialize

        [Test]
        public void InitializeRegistersILogQueryProviderTest()
        {
            Environment.SetEnvironmentVariable("Loki__BaseUrl", "http://loki.example.invalid:3100");
            try
            {
                var plugin = new LokiLogProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var provider = sp.GetService<ILogQueryProvider>();

                Assert.That(provider, Is.Not.Null);
                Assert.That(provider, Is.InstanceOf<LokiLogQueryProvider>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("Loki__BaseUrl", null);
            }
        }

        [Test]
        public void InitializeRegistersLokiOptionsTest()
        {
            Environment.SetEnvironmentVariable("Loki__BaseUrl", "http://loki.example.invalid:3100");
            Environment.SetEnvironmentVariable("Loki__TenantId", "witidentity");
            Environment.SetEnvironmentVariable("Loki__Username", "user");
            Environment.SetEnvironmentVariable("Loki__Password", "pw");
            try
            {
                var plugin = new LokiLogProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var options = sp.GetRequiredService<LokiOptions>();

                Assert.That(options.BaseUrl, Is.EqualTo("http://loki.example.invalid:3100"));
                Assert.That(options.TenantId, Is.EqualTo("witidentity"));
                Assert.That(options.Username, Is.EqualTo("user"));
                Assert.That(options.Password, Is.EqualTo("pw"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("Loki__BaseUrl", null);
                Environment.SetEnvironmentVariable("Loki__TenantId", null);
                Environment.SetEnvironmentVariable("Loki__Username", null);
                Environment.SetEnvironmentVariable("Loki__Password", null);
            }
        }

        [Test]
        public void InitializeThrowsWhenBaseUrlMissingTest()
        {
            // No env vars; appsettings.json ships with blank BaseUrl.
            var plugin = new LokiLogProviderPlugin();
            var services = new ServiceCollection();

            Assert.Throws<InvalidOperationException>(() => plugin.Initialize(services));
        }

        #endregion
    }
}
