using System;

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

            if (key.Length == 0)
                throw new ArgumentException("Secret key must not be empty.", nameof(key));

            if (key.Length > MAX_LENGTH)
                throw new ArgumentException(
                    $"Secret key is {key.Length} characters; the limit is {MAX_LENGTH}.",
                    nameof(key));

            for (int i = 0; i < key.Length; i++)
            {
                if (!IsValidChar(key[i]))
                    throw new ArgumentException(
                        $"Secret key carries an invalid character at position {i}; " +
                        "allowed are ASCII letters, digits, '.', '_', '/', '-'.",
                        nameof(key));
            }
        }

        /// <summary>
        /// Checks a key without throwing.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True when the key satisfies the rules.</returns>
        public static bool IsValid(string? key)
        {
            if (string.IsNullOrEmpty(key) || key.Length > MAX_LENGTH)
                return false;

            for (int i = 0; i < key.Length; i++)
            {
                if (!IsValidChar(key[i]))
                    return false;
            }

            return true;
        }

        private static bool IsValidChar(char c)
        {
            return c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
                or '.' or '_' or '/' or '-';
        }

        #endregion
    }
}
