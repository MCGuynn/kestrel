using System.Text;
using Kestrel.Core.Text;

namespace Kestrel.Tests;

public class TextFileCodecTests
{
    [Fact]
    public void Detects_utf8_bom() =>
        Assert.Equal(DocEncoding.Utf8Bom, TextFileCodec.DetectEncoding(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'a' }));

    [Fact]
    public void Detects_utf16_le_bom() =>
        Assert.Equal(DocEncoding.Utf16LeBom, TextFileCodec.DetectEncoding(new byte[] { 0xFF, 0xFE, (byte)'a', 0 }));

    [Fact]
    public void Detects_utf16_be_bom() =>
        Assert.Equal(DocEncoding.Utf16BeBom, TextFileCodec.DetectEncoding(new byte[] { 0xFE, 0xFF, 0, (byte)'a' }));

    [Fact]
    public void Valid_utf8_without_bom_is_utf8() =>
        Assert.Equal(DocEncoding.Utf8, TextFileCodec.DetectEncoding(Encoding.UTF8.GetBytes("caf\u00e9")));

    [Fact]
    public void Invalid_utf8_falls_back_to_ansi()
    {
        byte[] ansi = { (byte)'c', (byte)'a', (byte)'f', 0xE9 };
        Assert.Equal(DocEncoding.Ansi1252, TextFileCodec.DetectEncoding(ansi));
        Assert.Equal("caf\u00e9", TextFileCodec.Decode(ansi, DocEncoding.Ansi1252));
    }

    public static TheoryData<DocEncoding> Encodings => new()
    {
        DocEncoding.Utf8, DocEncoding.Utf8Bom, DocEncoding.Utf16LeBom, DocEncoding.Utf16BeBom, DocEncoding.Ansi1252,
    };

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Round_trips(DocEncoding encoding)
    {
        const string text = "SELECT 'caf\u00e9' FROM t;\r\n";
        Assert.Equal(text, TextFileCodec.Decode(TextFileCodec.Encode(text, encoding), encoding));
    }

    [Fact]
    public void Bom_is_written_only_when_requested()
    {
        Assert.Equal(4, TextFileCodec.Encode("a", DocEncoding.Utf8Bom).Length);
        Assert.Single(TextFileCodec.Encode("a", DocEncoding.Utf8));
    }

    [Fact]
    public void Detects_lossy_encoding()
    {
        Assert.False(TextFileCodec.CanEncodeLosslessly("\u65e5\u672c", DocEncoding.Ansi1252));
        Assert.True(TextFileCodec.CanEncodeLosslessly("\u65e5\u672c", DocEncoding.Utf8));
    }

    [Theory]
    [InlineData("a\r\nb\r\nc", EolMode.CrLf)]
    [InlineData("a\nb\n", EolMode.Lf)]
    [InlineData("a\rb", EolMode.Cr)]
    [InlineData("", EolMode.CrLf)]
    [InlineData("a\nb\nc\r\nd", EolMode.Lf)]
    public void Detects_eol(string text, EolMode expected) =>
        Assert.Equal(expected, TextFileCodec.DetectEol(text));

    [Fact]
    public void Atomic_write_round_trips_and_overwrites()
    {
        string path = Path.Combine(Path.GetTempPath(), $"kestrel-{Guid.NewGuid():N}.txt");
        try
        {
            TextFileCodec.WriteFileAtomic(path, "hello\nworld", DocEncoding.Utf8Bom);
            var (text, encoding, eol) = TextFileCodec.ReadFile(path);
            Assert.Equal("hello\nworld", text);
            Assert.Equal(DocEncoding.Utf8Bom, encoding);
            Assert.Equal(EolMode.Lf, eol);

            TextFileCodec.WriteFileAtomic(path, "second", DocEncoding.Utf8);
            Assert.Equal("second", TextFileCodec.ReadFile(path).Text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
