using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutWit.Common.Messenger;
using OutWit.Shared.Messenger.Provider.Telegram;
using OutWit.Shared.Messenger.Providers;

namespace OutWit.Shared.Messenger.Provider.Telegram.Tests
{
    [TestFixture]
    public class TelegramMessengerProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new TelegramMessengerProviderPlugin();

            Assert.That(plugin.Key, Is.EqualTo(TelegramMessengerProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("Telegram"));
        }

        [Test]
        public void PluginImplementsIMessengerProviderPluginTest()
        {
            var plugin = new TelegramMessengerProviderPlugin();

            Assert.That(plugin, Is.InstanceOf<IMessengerProviderPlugin>());
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void InitializeRegistersIMessengerTransportTest()
        {
            var plugin = new TelegramMessengerProviderPlugin();
            var services = new ServiceCollection();
            services.AddLogging();

            plugin.Initialize(services);

            var sp = services.BuildServiceProvider();
            var transport = sp.GetService<IMessengerTransport>();

            Assert.That(transport, Is.Not.Null);
            Assert.That(transport, Is.InstanceOf<TelegramMessengerTransport>());
        }

        #endregion
    }
}
