using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Messenger;
using OutWit.Shared.Messenger.Provider.Null;
using OutWit.Shared.Messenger.Providers;

namespace OutWit.Shared.Messenger.Provider.Null.Tests
{
    [TestFixture]
    public class NullMessengerProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new NullMessengerProviderPlugin();

            Assert.That(plugin.Key, Is.EqualTo(NullMessengerProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("Null"));
        }

        [Test]
        public void PluginImplementsIMessengerProviderPluginTest()
        {
            var plugin = new NullMessengerProviderPlugin();

            Assert.That(plugin, Is.InstanceOf<IMessengerProviderPlugin>());
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void InitializeRegistersIMessengerTransportTest()
        {
            var plugin = new NullMessengerProviderPlugin();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            plugin.Initialize(services);

            var sp = services.BuildServiceProvider();
            var transport = sp.GetService<IMessengerTransport>();

            Assert.That(transport, Is.Not.Null);
            Assert.That(transport, Is.InstanceOf<NullMessengerTransport>());
        }

        [Test]
        public void InitializeRegistersTransportAsSingletonTest()
        {
            var plugin = new NullMessengerProviderPlugin();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            plugin.Initialize(services);

            var sp = services.BuildServiceProvider();
            var a = sp.GetRequiredService<IMessengerTransport>();
            var b = sp.GetRequiredService<IMessengerTransport>();

            Assert.That(a, Is.SameAs(b));
        }

        #endregion
    }
}
