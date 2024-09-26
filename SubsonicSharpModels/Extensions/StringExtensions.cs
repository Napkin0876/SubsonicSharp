using System.Net;

namespace SubsonicSharp.Extensions;

public static class StringExtensions
{
    public static string Decode(this string encodedString)
    {
        return WebUtility.HtmlDecode(encodedString);
    }
}