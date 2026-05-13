using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Logging.Query;
using OutWit.Shared.Logging.Provider.File;
using OutWit.Shared.Logging.Providers;

namespace OutWit.Shared.Logging.Provider.File.Tests
{
    [TestFixture]
    public class FileLogProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new FileLogProviderPlugin();
            Assert.That(plugin.Key, Is.EqualTo(FileLogProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("File"));
        }

        [Test]
        public void PluginImplementsILogProviderPluginTest()
        {
            var plugin = new FileLogProviderPlugin();
            Assert.That(plugin, Is.InstanceOf<ILogProviderPlugin>());
        }

        #endregion

        #region Initialize

        [Test]
        public void InitializeRegistersILogQueryProviderTest()
        {
            var plugin = new FileLogProviderPlugin();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            plugin.Initialize(services);

            var sp = services.BuildServiceProvider();
            var provider = sp.GetService<ILogQueryProvider>();

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<FileLogQueryProvider>());
        }

        [Test]
        public void InitializePicksUpHostLoggingInfoTest()
        {
            var plugin = new FileLogProviderPlugin();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            // Host registers info before plugin initializes — happy path.
            services.AddSingleton(new HostLoggingInfo
            {
                LogsPath = "/app/logs",
                FilePattern = "log-*.json"
            });

            plugin.Initialize(services);

            // Provider factory runs lazily — just verify the DI graph builds and resolves.
            var sp = services.BuildServiceProvider();
            var provider = sp.GetService<ILogQueryProvider>();

            Assert.That(provider, Is.Not.Null);
            Assert.That(provider, Is.InstanceOf<FileLogQueryProvider>());
        }

        #endregion
    }
}
