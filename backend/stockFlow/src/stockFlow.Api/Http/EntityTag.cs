using stockFlow.Application.Common.Errors;

namespace stockFlow.Http;

// Utility for future update/post endpoints; not a global middleware.
public static class EntityTag
{
    public static string Format(byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length != 8)
            throw new ArgumentException("A SQL Server rowversion must contain eight bytes.", nameof(rowVersion));
        return "\"" + Convert.ToBase64String(rowVersion) + "\"";
    }

    public static byte[] RequireIfMatch(HttpRequest request)
    {
        var header = request.Headers.IfMatch;
        if (header.Count == 0 || string.IsNullOrWhiteSpace(header.ToString()))
            throw new AppException(ErrorKind.PreconditionRequired, "precondition_required", "If-Match is required.");
        var text = header.ToString().Trim();
        if (header.Count != 1 || text.Contains(',') || text.Length < 2 || text[0] != '"' || text[^1] != '"')
            throw new AppException(ErrorKind.Validation, "invalid_etag", "Supply one strong ETag in double quotes.");
        try
        {
            var bytes = Convert.FromBase64String(text[1..^1]);
            if (bytes.Length == 8 && text == Format(bytes)) return bytes;
        }
        catch (FormatException) { }
        throw new AppException(ErrorKind.Validation, "invalid_etag", "Supply a valid SQL rowversion ETag.");
    }

    public static void RequireMatches(HttpRequest request, byte[] currentVersion)
    {
        if (!RequireIfMatch(request).AsSpan().SequenceEqual(currentVersion))
            throw new AppException(ErrorKind.PreconditionFailed, "precondition_failed", "The resource changed. Reload it before retrying.");
    }
}
