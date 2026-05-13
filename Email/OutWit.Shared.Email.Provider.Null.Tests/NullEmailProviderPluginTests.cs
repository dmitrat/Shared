using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Null;
using OutWit.Shared.Email.Providers;

namespace OutWit.Shared.Email.Provider.Null.Tests
{
    [TestFixture]
    public class NullEmailProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new NullEmailProviderPlugin();

            Assert.That(plugin.Key, Is.EqualTo(NullEmailProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("Null"));
        }

        [Test]
        public void PluginImplementsIEmailProviderPluginTest()
        {
            var plugin = new NullEmailProviderPlugin();

            Assert.That(plugin, Is.InstanceOf<IEmailProviderPlugin>());
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void InitializeRegistersIEmailTransportTest()
        {
            var plugin = new NullEmailProviderPlugin();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            plugin.Initialize(services);

            var sp = services.BuildServiceProvider();
            var transport = sp.GetService<IEmailTransport>();

            Assert.That(transport, Is.Not.Null);
            Assert.That(transport, Is.InstanceOf<NullEmailTransport>());
        }

        [Test]
        public void InitializeRegistersTransportAsSingletonTest()
        {
            var plugin = new NullEmailProviderPlugin();
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            plugin.Initialize(services);

            var sp = services.BuildServiceProvider();
            var a = sp.GetRequiredService<IEmailTransport>();
            var b = sp.GetRequiredService<IEmailTransport>();

            Assert.That(a, Is.SameAs(b));
        }

        #endregion
    }
}
