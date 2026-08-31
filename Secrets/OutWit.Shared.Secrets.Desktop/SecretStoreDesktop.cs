using System;
using OutWit.Shared.Secrets.Provider.Linux;
using OutWit.Shared.Secrets.Provider.MacOS;
using OutWit.Shared.Secrets.Provider.Windows;
using OutWit.Shared.Secrets.Providers;

namespace OutWit.Shared.Secrets.Desktop
{
    /// <summary>
    /// The one-line answer for a cross-platform desktop application: the operating-system
    /// secret store for the platform this process runs on. Selecting among the OS providers
    /// by runtime platform is not silent degradation — on each desktop platform exactly one
    /// OS store is right, and all three protect with
    /// <see cref="SecretProtection.OperatingSystem"/>. Falling back to the File provider
    /// stays a deliberate, configured choice, which is why it is not part of this factory.
    /// </summary>
    public static class SecretStoreDesktop
    {
        #region Functions

        /// <summary>
        /// Creates the operating-system secret store for the current platform:
        /// Credential Manager on Windows, the login Keychain on macOS, the Secret Service
        /// on Linux. P/Invoke binds lazily, so the providers for the other platforms cost
        /// nothing at runtime.
        /// </summary>
        /// <returns>The store; log its <see cref="ISecretStore.Description"/> once at startup.</returns>
        /// <exception cref="PlatformNotSupportedException">No operating-system secret store
        /// exists for this platform; configure a provider explicitly.</exception>
        public static ISecretStore ForCurrentPlatform()
        {
            if (OperatingSystem.IsWindows())
                return new SecretStoreWindows();

            if (OperatingSystem.IsMacOS())
                return new SecretStoreKeychain();

            if (OperatingSystem.IsLinux())
                return new SecretStoreLibsecret();

            throw new PlatformNotSupportedException(
                "No operating-system secret store exists for this platform. " +
                "Configure a provider explicitly — the File provider is the deliberate " +
                "choice for platforms and deployments without one.");
        }

        #endregion
    }
}
