using Kestrel.Core.Languages;

namespace Kestrel.Tests;

public class LanguageCatalogTests
{
    [Theory]
    [InlineData("query.SQL", LanguageId.Sql)]
    [InlineData("C:/src/Program.cs", LanguageId.CSharp)]
    [InlineData("page.pcf", LanguageId.Xml)]
    [InlineData("README.md", LanguageId.Markdown)]
    [InlineData("app.tsx", LanguageId.TypeScript)]
    [InlineData("notes.xyz", LanguageId.PlainText)]
    [InlineData("Makefile", LanguageId.PlainText)]
    [InlineData(null, LanguageId.PlainText)]
    public void Maps_extensions(string? path, LanguageId expected) =>
        Assert.Equal(expected, LanguageCatalog.FromPath(path));

    [Fact]
    public void Every_language_has_a_display_name() =>
        Assert.All(LanguageCatalog.All, id => Assert.False(string.IsNullOrWhiteSpace(LanguageCatalog.DisplayName(id))));
}
