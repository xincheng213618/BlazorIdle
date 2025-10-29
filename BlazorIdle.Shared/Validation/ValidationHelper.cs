using System.Text.RegularExpressions;

namespace BlazorIdle.Shared.Validation;

public static class ValidationHelper
{
    /// <summary>
    /// Validates username format according to standard rules.
    /// Note: This method expects the username to already be sanitized (trimmed) via SanitizeUsername.
    /// Rules:
    /// - Only allows letters, numbers, underscore, dot, and hyphen
    /// - Cannot start or end with . _ -
    /// - Cannot contain consecutive special symbols (.. __ --)
    /// - No spaces, emojis, or other special symbols
    /// - Length must be 3-50 characters
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return (false, "用户名不能为空");
        }

        // Check length
        if (username.Length < 3 || username.Length > 50)
        {
            return (false, "用户名长度必须为3-50个字符");
        }

        // Check if starts or ends with special symbols (. _ -)
        if (Regex.IsMatch(username, @"^[._-]|[._-]$"))
        {
            return (false, "用户名不能以 . _ - 开头或结尾");
        }

        // Check for consecutive special symbols (.. __ --)
        if (Regex.IsMatch(username, @"\.{2,}|_{2,}|-{2,}"))
        {
            return (false, "用户名不能包含连续的特殊符号（如 .. __ --）");
        }

        // Check if only contains allowed characters (letters, numbers, underscore, dot, hyphen)
        // This also rejects spaces and other special characters
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$"))
        {
            return (false, "用户名只能包含字母、数字、点(.)、下划线(_)和连字符(-)");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Validates password format according to standard rules:
    /// - Can be any combination of letters, numbers, and symbols (ASCII standard characters only)
    /// - No accented characters
    /// - Minimum length of 6 characters
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "密码不能为空");
        }

        // Check minimum length
        if (password.Length < 6)
        {
            return (false, "密码长度至少为6个字符");
        }

        // Check if only contains ASCII printable characters (32-126)
        // This includes space, letters, numbers, and standard symbols
        foreach (char c in password)
        {
            if (c < 32 || c > 126)
            {
                return (false, "密码只能包含ASCII标准字符（字母、数字和符号）");
            }
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Sanitizes username by trimming whitespace
    /// </summary>
    public static string SanitizeUsername(string username)
    {
        return username?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Sanitizes password (currently just returns as-is, but can be extended)
    /// </summary>
    public static string SanitizePassword(string password)
    {
        return password ?? string.Empty;
    }
}
