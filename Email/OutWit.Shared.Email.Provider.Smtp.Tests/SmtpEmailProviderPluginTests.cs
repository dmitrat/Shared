using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Smtp;
using OutWit.Shared.Email.Providers;

namespace OutWit.Shared.Email.Provider.Smtp.Tests
{
    [TestFixture]
    public class SmtpEmailProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new SmtpEmailProviderPlugin();
            Assert.That(plugin.Key, Is.EqualTo(SmtpEmailProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("Smtp"));
        }

        [Test]
        public void PluginImplementsIEmailProviderPluginTest()
        {
            var plugin = new SmtpEmailProviderPlugin();
            Assert.That(plugin, Is.InstanceOf<IEmailProviderPlugin>());
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void InitializeRegistersIEmailTransportTest()
        {
            // The plugin reads its own appsettings.json. To exercise that path
            // we set an env var that overrides Smtp:Host so options validate.
            // The transport class throws if Host is empty.
            Environment.SetEnvironmentVariable("Smtp__Host", "smtp.example.com");
            try
            {
                var plugin = new SmtpEmailProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var transport = sp.GetService<IEmailTransport>();

                Assert.That(transport, Is.Not.Null);
                Assert.That(transport, Is.InstanceOf<SmtpEmailTransport>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("Smtp__Host", null);
            }
        }

        [Test]
        public void InitializeRegistersSmtpOptionsTest()
        {
            Environment.SetEnvironmentVariable("Smtp__Host", "smtp.example.com");
            Environment.SetEnvironmentVariable("Smtp__Port", "465");
            Environment.SetEnvironmentVariable("Smtp__Security", "SslOnConnect");
            try
            {
                var plugin = new SmtpEmailProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var options = sp.GetRequiredService<SmtpOptions>();

                Assert.That(options.Host, Is.EqualTo("smtp.example.com"));
                Assert.That(options.Port, Is.EqualTo(465));
                Assert.That(options.Security, Is.EqualTo(SmtpSecurity.SslOnConnect));
            }
            finally
            {
                Environment.SetEnvironmentVariable("Smtp__Host", null);
                Environment.SetEnvironmentVariable("Smtp__Port", null);
                Environment.SetEnvironmentVariable("Smtp__Security", null);
            }
        }

        #endregion
    }
}
