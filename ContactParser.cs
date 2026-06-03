using System.Text.RegularExpressions;

namespace TelephonyCallService;

public static class ContactParser
{
    private static readonly Regex XiPattern = new(@"x-i=([^;>\s]+)", RegexOptions.Compiled);

    public static string? ExtractXi(string contact)
    {
        var match = XiPattern.Match(contact);
        return match.Success ? match.Groups[1].Value : null;
    }
}
