namespace SafeVaultTests;

using System.Net;
using SafeVault.Helpers;

[TestClass]
public class TestInputValidation
{
    [TestMethod]
    public void TestForSQLInjection()
    {
        // Example of a SQL injection attempt.
        string maliciousInput = "Robert'); DROP TABLE Users;--";
        string sanitized = InputSanitizer.SanitizeInput(maliciousInput);

        // Assert that the sanitized input does not contain dangerous SQL keywords or patterns
        Assert.IsFalse(sanitized.Contains("DROP TABLE", StringComparison.OrdinalIgnoreCase), 
            "Sanitized input should not contain 'DROP TABLE'.");
        Assert.IsFalse(sanitized.Contains("--", StringComparison.OrdinalIgnoreCase), 
            "Sanitized input should not contain '--'.");
        Assert.IsFalse(sanitized.Contains("';", StringComparison.OrdinalIgnoreCase), 
            "Sanitized input should not contain SQL injection patterns like ';.");
        Assert.IsFalse(sanitized.Contains("'", StringComparison.OrdinalIgnoreCase), 
            "Sanitized input should not contain single quotes.");
    }

    [TestMethod]
    public void TestForXSS()
    {
        // A basic XSS attempt input.
        string maliciousInput = "<script>alert('XSS')</script>";
        string sanitized = InputSanitizer.SanitizeInput(maliciousInput);
        
        // Assert that the script tag is removed
        Assert.IsFalse(sanitized.ToLower().Contains("script"), "Input should be sanitized to remove script tags.");
        
        // Assert that inline event handlers are removed
        string inlineEventInput = "<img src='x' onerror='alert(\"XSS\")'>";
        string sanitizedInlineEvent = InputSanitizer.SanitizeInput(inlineEventInput);
        Assert.IsFalse(sanitizedInlineEvent.ToLower().Contains("onerror"), "Input should be sanitized to remove inline event handlers.");
        
        // Assert that encoded script tags are removed
        string encodedInput = "&#x3C;script&#x3E;alert('XSS')&#x3C;/script&#x3E;";
        string sanitizedEncoded = InputSanitizer.SanitizeInput(encodedInput);
        Assert.IsFalse(sanitizedEncoded.ToLower().Contains("script"), "Input should be sanitized to remove encoded script tags.");
    }

    [TestMethod]
    public void TestForValidEmailAndUsername()
    {
        // Test with properly formatted inputs.
        string username = "Valid_User123";
        string email = "user@example.com";
        var (sanitizedUsername, sanitizedEmail, valid) = InputSanitizer.ValidateAndSanitize(username, email);
        
        Assert.IsTrue(valid, "Proper inputs should validate successfully.");
        Assert.AreEqual("Valid_User123", WebUtility.HtmlDecode(sanitizedUsername)); // decode to compare original
        Assert.AreEqual("user@example.com", WebUtility.HtmlDecode(sanitizedEmail));
    }
}
