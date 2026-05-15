using System.Globalization;
using System.Security.Claims;

namespace OutWit.Shared.Blazor.Shell.Utils
{
    /// <summary>
    /// Utility methods for displaying user identity information.
    /// </summary>
    public static class DisplayUtils
    {
        #region Functions

        /// <summary>
        /// Extracts initials from a display name or username.
        /// </summary>
        /// <param name="name">Name to extract initials from.</param>
        /// <returns>1-2 character uppercase initials, or <c>"U"</c> for empty input.</returns>
        public static string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "U";

            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return parts.Length == 1
                ? parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant()
                : $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        /// <summary>
        /// Extracts a display name from standard OIDC claims.
        /// </summary>
        /// <param name="user">The authenticated user's claims principal.</param>
        /// <returns>Title-cased display name, falling back through multiple claim types.</returns>
        public static string GetDisplayName(ClaimsPrincipal user)
        {
            var name = user.FindFirst("name")?.Value
                       ?? user.Identity?.Name
                       ?? user.FindFirst("preferred_username")?.Value
                       ?? user.FindFirst("unique_name")?.Value
                       ?? user.FindFirst(ClaimTypes.Name)?.Value
                       ?? user.FindFirst("sub")?.Value
                       ?? "User";

            return ToTitleCaseSafe(name);
        }

        /// <summary>
        /// Converts a string to title case, handling exceptions gracefully.
        /// </summary>
        /// <param name="input">Input string to convert.</param>
        /// <returns>Title-cased string, or original input if conversion fails.</returns>
        public static string ToTitleCaseSafe(string input)
        {
            try
            {
                var lower = input.ToLowerInvariant();
                var textInfo = CultureInfo.CurrentUICulture.TextInfo;
                return textInfo.ToTitleCase(lower);
            }
            catch
            {
                return input;
            }
        }

        #endregion
    }
}
