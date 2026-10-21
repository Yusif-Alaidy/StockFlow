using System;
namespace stockFlow.Domain.Common;

// Rules shared by the new documents; does not change Phase 02 Guard.
internal static class DocumentRules
{
    internal static Guid Id(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("An identifier is required.");
        return value;
    }
    internal static string Text(string value, int limit)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > limit)
            throw new ArgumentException($"Text is required and must not exceed {limit} characters.");
        return value.Trim();
    }
    internal static string? Notes(string? value) => string.IsNullOrWhiteSpace(value) ? null : Text(value, 1000);
    internal static DateTime Utc(DateTime value)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("A non-default UTC timestamp is required.");
        return value;
    }
    internal static void Time(DateTime value, DateTime latest)
    {
        Utc(value);
        if (value < latest) throw new ArgumentException("Timestamp cannot precede the latest change.");
    }
    internal static decimal Quantity(decimal value)
    {
        if (value <= 0 || value > 999999999999999.999m || decimal.Round(value, 3) != value)
            throw new ArgumentOutOfRangeException(nameof(value), "Quantity must be positive and fit decimal(18,3).");
        return value;
    }
    internal static decimal Price(decimal value)
    {
        if (value < 0 || value > 9999999999999999.99m || decimal.Round(value, 2) != value)
            throw new ArgumentOutOfRangeException(nameof(value), "Price must be nonnegative and fit decimal(18,2).");
        return value;
    }
    internal static decimal LineTotal(decimal quantity, decimal price)
    {
        var total = decimal.Round(checked(quantity * price), 2, MidpointRounding.AwayFromZero);
        return Price(total);
    }
}
