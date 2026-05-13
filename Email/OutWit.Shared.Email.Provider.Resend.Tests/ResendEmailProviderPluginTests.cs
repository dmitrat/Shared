using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Resend;
using OutWit.Shared.Email.Providers;
using Resend;

namespace OutWit.Shared.Email.Provider.Resend.Tests
{
    [TestFixture]
    public class ResendEmailProviderPluginTests
    {
        #region Key Tests

        [Test]
        public void KeyMatchesConstantTest()
        {
            var plugin = new ResendEmailProviderPlugin();
            Assert.That(plugin.Key, Is.EqualTo(ResendEmailProviderPlugin.KEY));
            Assert.That(plugin.Key, Is.EqualTo("Resend"));
        }

        [Test]
        public void PluginImplementsIEmailProviderPluginTest()
        {
            var plugin = new ResendEmailProviderPlugin();
            Assert.That(plugin, Is.InstanceOf<IEmailProviderPlugin>());
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void InitializeRegistersIEmailTransportTest()
        {
            // The plugin reads its own appsettings.json. Env var override supplies
            // a fake API token so DI graph builds — no HTTP call is made until SendAsync.
            Environment.SetEnvironmentVariable("Resend__ApiToken", "re_test_token_12345");
            try
            {
                var plugin = new ResendEmailProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var transport = sp.GetService<IEmailTransport>();

                Assert.That(transport, Is.Not.Null);
                Assert.That(transport, Is.InstanceOf<ResendEmailTransport>());
            }
            finally
            {
                Environment.SetEnvironmentVariable("Resend__ApiToken", null);
            }
        }

        [Test]
        public void InitializeBindsApiTokenFromEnvVarTest()
        {
            Environment.SetEnvironmentVariable("Resend__ApiToken", "re_env_override_value");
            try
            {
                var plugin = new ResendEmailProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var opts = sp.GetRequiredService<IOptions<ResendClientOptions>>().Value;

                Assert.That(opts.ApiToken, Is.EqualTo("re_env_override_value"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("Resend__ApiToken", null);
            }
        }

        [Test]
        public void InitializeRegistersResendOptionsTest()
        {
            Environment.SetEnvironmentVariable("Resend__ApiToken", "re_test_token");
            Environment.SetEnvironmentVariable("Resend__ApiUrl", "https://staging.resend.com");
            try
            {
                var plugin = new ResendEmailProviderPlugin();
                var services = new ServiceCollection();
                services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

                plugin.Initialize(services);

                var sp = services.BuildServiceProvider();
                var options = sp.GetRequiredService<ResendOptions>();

                Assert.That(options.ApiToken, Is.EqualTo("re_test_token"));
                Assert.That(options.ApiUrl, Is.EqualTo("https://staging.resend.com"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("Resend__ApiToken", null);
                Environment.SetEnvironmentVariable("Resend__ApiUrl", null);
            }
        }

        #endregion
    }
}
