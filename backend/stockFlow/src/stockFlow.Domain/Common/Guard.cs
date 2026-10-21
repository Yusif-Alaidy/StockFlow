using System;
using System.Net.Mail;

namespace stockFlow.Domain.Common;

internal static class Guard
{
    public static string RequiredText(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A non-empty value is required.", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Maximum length is {maxLength} characters.", parameterName);

        return normalized;
    }

    public static string Code(string? value, int maxLength, string parameterName)
    {
        var normalized = RequiredText(value, maxLength, parameterName).ToUpperInvariant();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"Maximum length is {maxLength} characters.", parameterName);

        return normalized;
    }

    public static Guid RequiredId(Guid value, string parameterName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("The identifier cannot be empty.", parameterName);

        return value;
    }

    public static string? OptionalText(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return RequiredText(value, maxLength, parameterName);
    }

    public static string? Email(string? value)
    {
        var normalized = OptionalText(value, FieldLimits.Email, nameof(value));
        if (normalized is null)
            return null;

        // Syntax only: this does not prove mailbox ownership or deliverability.
        if (normalized.Contains('\r') || normalized.Contains('\n') ||
            !MailAddress.TryCreate(normalized, out var address) ||
            !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A plain email address is required.", nameof(value));

        return normalized;
    }

    public static DateTime Utc(DateTime value, string parameterName)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A non-default UTC timestamp is required.", parameterName);

        return value;
    }
}
