using System;
using System.Security.Cryptography;
using System.Text;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// The key rules, validated in the abstraction so every provider sees the same thing.
    /// Convention: "{Product}/{Purpose}", e.g. "Norav.Bridge/AgentCredential". Case-sensitive,
    /// ASCII letters, digits, '.', '_', '/', '-', 1–128 characters. A key that breaks this
    /// throws, because it is a bug in the caller, not an operational event.
    /// </summary>
    public static class SecretKeys
    {
        #region Constants

        /// <summary>
        /// Maximum key length, in characters.
        /// </summary>
        public const int MAX_LENGTH = 128;

        #endregion

        #region Functions

        /// <summary>
        /// Validates a key, throwing for a caller bug.
        /// </summary>
        /// <param name="key">The key to validate.</param>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key is empty, too long, or carries a
        /// character outside [A-Za-z0-9._/-].</exception>
        public static void Validate(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            string? violation = FindViolation(key);
            if (violation != null)
                throw new ArgumentException(violation, nameof(key));
        }

        /// <summary>
        /// Checks a key without throwing. The rules are the same ones
        /// <see cref="Validate"/> enforces — there is exactly one copy of them.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True when the key satisfies the rules.</returns>
        public static bool IsValid(string? key)
        {
            return key != null && FindViolation(key) == null;
        }

        /// <summary>
        /// The canonical case-disambiguation fingerprint: the first 8 lowercase hex
        /// characters of SHA-256 over the UTF-8 key. Providers whose platform namespace is
        /// case-insensitive (Windows Credential Manager target names, file names) append it
        /// so the mapping stays injective; support tooling recomputes it to locate an entry.
        /// This recipe is a durability contract — stored entries are found by it — so it has
        /// exactly one definition.
        /// </summary>
        /// <param name="key">The secret's key.</param>
        /// <returns>Eight lowercase hex characters.</returns>
        /// <exception cref="ArgumentNullException">The key is null.</exception>
        /// <exception cref="ArgumentException">The key breaks the rules.</exception>
        public static string Fingerprint(string key)
        {
            Validate(key);

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
        }

        private static string? FindViolation(string key)
        {
            if (key.Length == 0)
                return "Secret key must not be empty.";

            if (key.Length > MAX_LENGTH)
                return $"Secret key is {key.Length} characters; the limit is {MAX_LENGTH}.";

            for (int i = 0; i < key.Length; i++)
            {
                if (!IsValidChar(key[i]))
                    return $"Secret key carries an invalid character at position {i}; " +
                           "allowed are ASCII letters, digits, '.', '_', '/', '-'.";
            }

            return null;
        }

        private static bool IsValidChar(char c)
        {
            return c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
                or '.' or '_' or '/' or '-';
        }

        #endregion
    }
}
