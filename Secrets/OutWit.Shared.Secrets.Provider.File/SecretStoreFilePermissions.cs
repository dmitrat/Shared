using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

namespace OutWit.Shared.Secrets.Provider.File
{
    /// <summary>
    /// Restrictive permissions at creation, verified by reading them back. Creating a file
    /// and hoping the parent directory's ACL is sane is precisely how a ProgramData subtree
    /// ends up letting any authenticated user replace a credential.
    /// </summary>
    internal static class SecretStoreFilePermissions
    {
        #region Constants

        private const UnixFileMode OWNER_ONLY = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        #endregion

        #region Functions

        /// <summary>
        /// Creates (or truncates) a file readable and writable by the owning account only:
        /// 0600 on POSIX, an explicit non-inherited DACL naming only the current account on
        /// Windows.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The open stream, write-through.</returns>
        internal static FileStream CreateRestricted(string path)
        {
            if (OperatingSystem.IsWindows())
                return CreateWindows(path);

            var options = new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.WriteThrough,
                UnixCreateMode = OWNER_ONLY
            };

            return new FileStream(path, options);
        }

        /// <summary>
        /// Reads the permissions back and names the violation, if any.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>Null when the file is owner-only; otherwise what is wrong, in words a
        /// support engineer can act on.</returns>
        internal static string? Verify(string path)
        {
            if (OperatingSystem.IsWindows())
                return VerifyWindowsAcl(path);

            UnixFileMode mode = System.IO.File.GetUnixFileMode(path);

            const UnixFileMode wider = UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                                       UnixFileMode.GroupExecute | UnixFileMode.OtherRead |
                                       UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

            if ((mode & wider) != 0)
                return $"The file's mode is {Convert.ToString((int)mode, 8)}; " +
                       "group or other bits are set where 600 was required.";

            return null;
        }

        [SupportedOSPlatform("windows")]
        private static FileStream CreateWindows(string path)
        {
            SecurityIdentifier? user = WindowsIdentity.GetCurrent().User;
            if (user == null)
                throw new InvalidOperationException("The current Windows identity has no SID.");

            var security = new FileSecurity();
            security.SetAccessRuleProtection(true, false);
            security.AddAccessRule(new FileSystemAccessRule(user,
                FileSystemRights.FullControl, AccessControlType.Allow));

            // The DACL is part of the create call: there is no window in which the file
            // exists with the directory's inherited permissions.
            return new FileInfo(path).Create(FileMode.Create,
                FileSystemRights.FullControl, FileShare.None, 4096,
                FileOptions.WriteThrough, security);
        }

        [SupportedOSPlatform("windows")]
        private static string? VerifyWindowsAcl(string path)
        {
            var fileInfo = new FileInfo(path);
            FileSecurity security = fileInfo.GetAccessControl();

            if (!security.AreAccessRulesProtected)
                return "The file inherits permissions from its directory; an explicit DACL was required.";

            SecurityIdentifier? user = WindowsIdentity.GetCurrent().User;

            foreach (FileSystemAccessRule rule in
                     security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            {
                if (!rule.IdentityReference.Equals(user))
                    return $"The file's DACL names '{rule.IdentityReference.Value}' where only " +
                           "the owning account was required.";
            }

            return null;
        }

        #endregion
    }
}
