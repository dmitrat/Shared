namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// What is actually protecting a stored secret. A host logs this once at startup and can
    /// refuse to run on <see cref="FileOnly"/> in a configuration that requires better; without
    /// it, the difference between a properly protected deployment and a degraded one is
    /// invisible in every log anyone will ever read.
    /// </summary>
    public enum SecretProtection : byte
    {
        /// <summary>
        /// Not stated. A provider must never report this.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The operating system's credential store, protected by the account's keys.
        /// </summary>
        OperatingSystem = 1,

        /// <summary>
        /// A file this process wrote, protected by its ACL and a platform key
        /// (e.g. Windows DPAPI machine scope).
        /// </summary>
        FileWithPlatformKey = 2,

        /// <summary>
        /// A file this process wrote, protected only by its ACL.
        /// </summary>
        FileOnly = 3
    }
}
