using System;
using System.Runtime.InteropServices;

namespace OutWit.Shared.Secrets.Provider.MacOS
{
    /// <summary>
    /// Security-framework interop over the stable SecKeychain generic-password API — the same
    /// route MSAL.Extensions takes; the SecItem/CFDictionary dance buys nothing here. Binary
    /// payloads are first-class (length + bytes), so no encoding is needed.
    /// </summary>
    internal static partial class SecretStoreKeychainNative
    {
        #region Constants

        internal const int ERR_SEC_SUCCESS = 0;
        internal const int ERR_SEC_NOT_AVAILABLE = -25291;
        internal const int ERR_SEC_AUTH_FAILED = -25293;
        internal const int ERR_SEC_DUPLICATE_ITEM = -25299;
        internal const int ERR_SEC_ITEM_NOT_FOUND = -25300;
        internal const int ERR_SEC_NO_DEFAULT_KEYCHAIN = -25307;
        internal const int ERR_SEC_INTERACTION_NOT_ALLOWED = -25308;

        private const string SECURITY =
            "/System/Library/Frameworks/Security.framework/Security";

        private const string CORE_FOUNDATION =
            "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        #endregion

        #region Functions

        [LibraryImport(SECURITY)]
        internal static partial int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            uint passwordLength, byte[] passwordData,
            out IntPtr itemRef);

        [LibraryImport(SECURITY)]
        internal static partial int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            out uint passwordLength, out IntPtr passwordData,
            out IntPtr itemRef);

        /// <summary>
        /// Locates an item without requesting its secret: NULL password out-pointers mean no
        /// decrypt, and therefore no keychain ACL check or user prompt just to find the item —
        /// the store and delete paths need the reference, not the data.
        /// </summary>
        [LibraryImport(SECURITY, EntryPoint = "SecKeychainFindGenericPassword")]
        internal static partial int SecKeychainFindGenericPasswordRef(
            IntPtr keychainOrArray,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            IntPtr passwordLength, IntPtr passwordData,
            out IntPtr itemRef);

        [LibraryImport(SECURITY)]
        internal static partial int SecKeychainItemModifyAttributesAndData(
            IntPtr itemRef, IntPtr attrList, uint length, byte[] data);

        [LibraryImport(SECURITY)]
        internal static partial int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

        [LibraryImport(SECURITY)]
        internal static partial int SecKeychainItemDelete(IntPtr itemRef);

        [LibraryImport(CORE_FOUNDATION)]
        internal static partial void CFRelease(IntPtr cf);

        #endregion
    }
}
