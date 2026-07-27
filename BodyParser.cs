namespace TelephonyCallService;

public static class BodyParser
{
    public static (string? contact, string? from) ParseRaw(string body)
    {
        var contact = ExtractSimpleField(body, "contact");
        var from = ExtractFromField(body);
        return (contact, from);
    }

    private static string? ExtractSimpleField(string body, string fieldName)
    {
        var marker = $"\"{fieldName}\":\"";
        var start = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        start += marker.Length;
        var end = body.IndexOf('"', start);
        if (end < 0) return null;
        return body[start..end];
    }

    // from value may contain unescaped quotes so we find the last " before } or ,
    private static string? ExtractFromField(string body)
    {
        var marker = "\"from\":\"";
        var start = body.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        start += marker.Length;

        var remaining = body[start..];
        var lastQuote = remaining.LastIndexOf('"');
        if (lastQuote < 0) return null;

        var afterQuote = remaining[(lastQuote + 1)..].TrimStart();
        if (afterQuote.StartsWith('}') || afterQuote.StartsWith(','))
            return remaining[..lastQuote];

        return null;
    }
}
