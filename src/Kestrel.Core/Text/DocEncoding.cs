using System.Text;

namespace Kestrel.Core.Text;

/// <summary>An encoding choice shown in the UI. Value equality makes instances safe to compare.</summary>
public sealed record DocEncoding(string DisplayName, int CodePage, bool HasBom)
{
    public static readonly DocEncoding Utf8 = new("UTF-8", 65001, false);
    public static readonly DocEncoding Utf8Bom = new("UTF-8 BOM", 65001, true);
    public static readonly DocEncoding Utf16LeBom = new("UTF-16 LE BOM", 1200, true);
    public static readonly DocEncoding Utf16BeBom = new("UTF-16 BE BOM", 1201, true);
    public static readonly DocEncoding Ansi1252 = new("ANSI (Windows-1252)", 1252, false);

    public static IReadOnlyList<DocEncoding> All { get; } = [Utf8, Utf8Bom, Utf16LeBom, Utf16BeBom, Ansi1252];

    static DocEncoding()
    {
        // Legacy code pages such as 1252 are not available on .NET 8 without this provider.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Encoding GetEncoding() => CodePage switch
    {
        65001 => new UTF8Encoding(HasBom, throwOnInvalidBytes: false),
        1200 => new UnicodeEncoding(bigEndian: false, byteOrderMark: HasBom),
        1201 => new UnicodeEncoding(bigEndian: true, byteOrderMark: HasBom),
        _ => Encoding.GetEncoding(CodePage),
    };
}
