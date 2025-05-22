using System;
using System.Text.RegularExpressions;
using System.Net;

namespace SafeVault.Helpers
{
    public static class InputSanitizer
    {
        /// <summary>
        /// Removes script tags, HTML, and any malicious patterns then encodes the result.
        /// </summary>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Decode HTML-encoded input to handle encoded tags like &#x3C;script&#x3E;
            string decodedInput = WebUtility.HtmlDecode(input);

            // Remove any script tags and their content (e.g., <script>alert('XSS')</script>)
            string output = Regex.Replace(decodedInput, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);

            // Remove all remaining HTML tags
            output = Regex.Replace(output, @"<[^>]+>", "");

            // Optionally remove inline event handlers (like onerror, onclick)
            output = Regex.Replace(output, @"on\w+\s*=\s*""[^""]+""", "", RegexOptions.IgnoreCase);

            // Remove common SQL injection patterns
            output = Regex.Replace(output, @"([';])", "", RegexOptions.IgnoreCase);

            // Remove dangerous SQL keywords
            string[] sqlKeywords = { "DROP", "TABLE", "INSERT", "DELETE", "UPDATE", "SELECT", ";" };
            foreach (var keyword in sqlKeywords)
            {
                output = Regex.Replace(output, $@"\b{keyword}\b", "", RegexOptions.IgnoreCase);
            }

            // Remove double hyphens separately
            output = Regex.Replace(output, @"--", "", RegexOptions.IgnoreCase);

            // Finally, HTML-encode the output so that any remaining special characters become safe
            output = WebUtility.HtmlEncode(output);

            return output;
        }

        /// <summary>
        /// Validates email format using System.Net.Mail.
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates and sanitizes the username and email.
        /// For usernames, this example permits only alphanumeric characters, underscores, and hyphens.
        /// </summary>
        public static (string sanitizedUsername, string sanitizedEmail, bool valid) ValidateAndSanitize(string username, string email)
        {
            // Sanitize inputs
            string sanitizedUsername = SanitizeInput(username);
            string sanitizedEmail = SanitizeInput(email);

            // Validate: restrict the username to safe characters and check email format
            bool usernameValid = Regex.IsMatch(sanitizedUsername, @"^[a-zA-Z0-9_\-]+$");
            bool emailValid = IsValidEmail(sanitizedEmail);
            bool valid = usernameValid && emailValid;

            return (sanitizedUsername, sanitizedEmail, valid);
        }
    }
}