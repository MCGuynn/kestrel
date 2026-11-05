using System.Text;

namespace Kestrel.Core.Text;

public enum EolMode { CrLf, Lf, Cr }

public sealed class FileTooLargeException(long size, long limit)
    : IOException($"File is {size / (1024 * 1024)} MB; the limit is {limit / (1024 * 1024)} MB.");

/// <summary>Byte-level file handling: encoding and line-ending detection, decoding, and atomic writes.</summary>
public static class TextFileCodec
{
    public const long MaxFileBytes = 256L * 1024 * 1024;

    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static DocEncoding DetectEncoding(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return DocEncoding.Utf8Bom;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return DocEncoding.Utf16LeBom;
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return DocEncoding.Utf16BeBom;
        return IsValidUtf8(bytes) ? DocEncoding.Utf8 : DocEncoding.Ansi1252;
    }

    private static bool IsValidUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            StrictUtf8.GetCharCount(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    public static string Decode(byte[] bytes, DocEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var enc = encoding.GetEncoding();
        int skip = 0;
        if (encoding.HasBom)
        {
            byte[] preamble = enc.GetPreamble();
            if (bytes.AsSpan().StartsWith(preamble)) skip = preamble.Length;
        }
        return enc.GetString(bytes, skip, bytes.Length - skip);
    }

    public static byte[] Encode(string text, DocEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(text);
        var enc = encoding.GetEncoding();
        byte[] preamble = encoding.HasBom ? enc.GetPreamble() : [];
        byte[] body = enc.GetBytes(text);
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }

    /// <summary>False when saving in this encoding would replace characters (e.g. CJK text saved as Windows-1252).</summary>
    public static bool CanEncodeLosslessly(string text, DocEncoding encoding) =>
        Decode(Encode(text, encoding), encoding) == text;

    public static EolMode DetectEol(string text)
    {
        int crlf = 0, lf = 0, cr = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n') { crlf++; i++; }
                else cr++;
            }
            else if (c == '\n')
            {
                lf++;
            }
        }
        if (crlf == 0 && lf == 0 && cr == 0) return EolMode.CrLf;
        if (crlf >= lf && crlf >= cr) return EolMode.CrLf;
        return lf >= cr ? EolMode.Lf : EolMode.Cr;
    }

    public static string EolString(EolMode mode) => mode switch
    {
        EolMode.Lf => "\n",
        EolMode.Cr => "\r",
        _ => "\r\n",
    };

    public static (string Text, DocEncoding Encoding, EolMode Eol) ReadFile(string path, DocEncoding? forced = null)
    {
        var info = new FileInfo(path);
        if (info.Length > MaxFileBytes) throw new FileTooLargeException(info.Length, MaxFileBytes);
        byte[] bytes = File.ReadAllBytes(path);
        var encoding = forced ?? DetectEncoding(bytes);
        string text = Decode(bytes, encoding);
        return (text, encoding, DetectEol(text));
    }

    /// <summary>Writes to a temp file in the same folder, then swaps it in, so a crash never leaves a half-written file.</summary>
    public static void WriteFileAtomic(string path, string text, DocEncoding encoding)
    {
        byte[] bytes = Encode(text, encoding);
        string full = Path.GetFullPath(path);
        string dir = Path.GetDirectoryName(full)!;
        string temp = Path.Combine(dir, $".{Path.GetFileName(full)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temp, bytes);
            if (File.Exists(full)) File.Replace(temp, full, destinationBackupFileName: null, ignoreMetadataErrors: true);
            else File.Move(temp, full);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
