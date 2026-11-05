using Kestrel.Core.Languages;
using ScintillaNET;

namespace Kestrel.App.Highlighting;

/// <summary>Maps each language's Lexilla styles onto theme roles. All Scintilla lexer setup lives here.</summary>
internal static class LanguageStyler
{
    private const string SqlKeywords = "select from where and or not in is null like ilike between exists as on join inner left right full outer cross natural using group by order having limit offset fetch first next rows only union all intersect except distinct insert into values update set delete returning create alter drop table view materialized index unique primary key foreign references constraint check default cascade restrict schema sequence function procedure returns language plpgsql begin end declare if then else elsif case when loop for while return raise notice exception perform execute grant revoke with recursive over partition window filter lateral true false asc desc nulls cast coalesce nullif greatest least truncate comment trigger before after each row instead of do vacuum analyze explain";
    private const string PgTypes = "integer int int2 int4 int8 smallint bigint serial bigserial numeric decimal real double precision float4 float8 money text varchar char character varying boolean bool date time timestamp timestamptz interval uuid json jsonb bytea inet cidr macaddr array tsvector tsquery xml point void record";
    private const string CSharpKeywords = "abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly record ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using var virtual void volatile while async await yield get set init value when where nameof global required file scoped";
    private const string CSharpTypes = "List Dictionary HashSet Task IEnumerable IReadOnlyList Action Func String Int32 Exception Console Math DateTime Guid Span ReadOnlySpan";
    private const string JavaKeywords = "abstract assert boolean break byte case catch char class const continue default do double else enum extends final finally float for goto if implements import instanceof int interface long native new package private protected public return short static strictfp super switch synchronized this throw throws transient try void volatile while true false null var record sealed permits yield";
    private const string JavaTypes = "String Integer Long Double Boolean Object List Map Set ArrayList HashMap Optional Stream System Exception";
    private const string JsKeywords = "break case catch class const continue debugger default delete do else export extends finally for function if import in instanceof let new return super switch this throw try typeof var void while with yield async await of static get set true false null undefined NaN Infinity";
    private const string JsTypes = "Array Object String Number Boolean Promise Map Set JSON Math Date RegExp Error console window document globalThis";
    private const string TsExtraKeywords = " interface type enum implements namespace declare abstract readonly private protected public keyof infer is as satisfies unknown never any";
    private const string TsExtraTypes = " string number boolean object bigint symbol Record Partial Readonly Pick Omit";
    private const string PythonKeywords = "False None True and as assert async await break class continue def del elif else except finally for from global if import in is lambda nonlocal not or pass raise return try while with yield match case";
    private const string PythonBuiltins = "print len range str int float list dict set tuple bool open enumerate zip map filter sorted reversed sum min max abs isinstance type super self cls object Exception ValueError TypeError KeyError";
    private const string HtmlTags = "html head body title meta link script style div span p a img ul ol li table thead tbody tr th td form input button select option textarea label h1 h2 h3 h4 h5 h6 header footer nav main section article aside br hr pre code strong em b i u small iframe svg canvas video audio source template slot dialog details summary figure figcaption picture noscript";

    public static void Apply(Scintilla s, LanguageId language, Theme t)
    {
        ApplyBase(s, t);
        switch (language)
        {
            case LanguageId.Sql: Sql(s, t); break;
            case LanguageId.CSharp: CFamily(s, t, CSharpKeywords, CSharpTypes, backquotes: false); break;
            case LanguageId.Java: CFamily(s, t, JavaKeywords, JavaTypes, backquotes: false); break;
            case LanguageId.JavaScript: CFamily(s, t, JsKeywords, JsTypes, backquotes: true); break;
            case LanguageId.TypeScript: CFamily(s, t, JsKeywords + TsExtraKeywords, JsTypes + TsExtraTypes, backquotes: true); break;
            case LanguageId.Python: Python(s, t); break;
            case LanguageId.Html: Markup(s, t, "hypertext", HtmlTags); break;
            case LanguageId.Xml: Markup(s, t, "xml", ""); break;
            case LanguageId.Css: Css(s, t); break;
            case LanguageId.Json: Json(s, t); break;
            case LanguageId.Markdown: Markdown(s, t); break;
            default: s.LexerName = "null"; break;
        }
        s.SetProperty("fold", "1");
        s.SetProperty("fold.compact", "0");
        s.SetProperty("fold.comment", "1");
        s.SetProperty("fold.html", "1");
        s.SetProperty("fold.preprocessor", "1");
        s.Colorize(0, -1);
    }

    private static void ApplyBase(Scintilla s, Theme t)
    {
        s.StyleResetDefault();
        var d = s.Styles[Style.Default];
        d.Font = "Consolas";
        d.Size = 11;
        d.BackColor = t.Back;
        d.ForeColor = t.Fore;
        s.StyleClearAll();

        s.Styles[Style.LineNumber].BackColor = t.MarginBack;
        s.Styles[Style.LineNumber].ForeColor = t.MarginFore;
        s.Styles[Style.IndentGuide].ForeColor = t.MarginFore;
        s.CaretForeColor = t.Caret;
        s.CaretLineBackColor = t.CurrentLine;
        s.CaretLineVisible = true;
        s.SetSelectionBackColor(true, t.Selection);
        s.SetFoldMarginColor(true, t.MarginBack);
        s.SetFoldMarginHighlightColor(true, t.MarginBack);
        for (int i = Marker.FolderEnd; i <= Marker.FolderOpen; i++)
        {
            s.Markers[i].SetForeColor(t.MarginBack);
            s.Markers[i].SetBackColor(t.MarginFore);
        }
    }

    private static void Map(Scintilla s, Theme t, TokenRole role, params int[] styles)
    {
        var color = t.Tokens[role];
        foreach (int id in styles) s.Styles[id].ForeColor = color;
    }

    private static void Sql(Scintilla s, Theme t)
    {
        s.LexerName = "sql";
        Map(s, t, TokenRole.Comment, Style.Sql.Comment, Style.Sql.CommentLine, Style.Sql.CommentDoc, Style.Sql.CommentLineDoc);
        Map(s, t, TokenRole.Number, Style.Sql.Number);
        Map(s, t, TokenRole.Keyword, Style.Sql.Word);
        Map(s, t, TokenRole.Keyword2, Style.Sql.Word2);
        Map(s, t, TokenRole.String, Style.Sql.String, Style.Sql.Character);
        Map(s, t, TokenRole.Operator, Style.Sql.Operator);
        Map(s, t, TokenRole.Attribute, Style.Sql.QuotedIdentifier);
        s.Styles[Style.Sql.Word].Bold = true;
        s.SetKeywords(0, SqlKeywords);
        s.SetKeywords(1, PgTypes);
    }

    private static void CFamily(Scintilla s, Theme t, string keywords, string types, bool backquotes)
    {
        s.LexerName = "cpp";
        if (backquotes) s.SetProperty("lexer.cpp.backquoted.strings", "1");
        Map(s, t, TokenRole.Comment, Style.Cpp.Comment, Style.Cpp.CommentLine, Style.Cpp.CommentDoc, Style.Cpp.CommentLineDoc);
        Map(s, t, TokenRole.Number, Style.Cpp.Number);
        Map(s, t, TokenRole.Keyword, Style.Cpp.Word);
        Map(s, t, TokenRole.Keyword2, Style.Cpp.Word2);
        Map(s, t, TokenRole.String, Style.Cpp.String, Style.Cpp.Character, Style.Cpp.Verbatim, Style.Cpp.StringRaw, Style.Cpp.StringEol);
        Map(s, t, TokenRole.Operator, Style.Cpp.Operator);
        Map(s, t, TokenRole.Preprocessor, Style.Cpp.Preprocessor);
        Map(s, t, TokenRole.Regex, Style.Cpp.Regex);
        s.SetKeywords(0, keywords);
        s.SetKeywords(1, types);
    }

    private static void Python(Scintilla s, Theme t)
    {
        s.LexerName = "python";
        Map(s, t, TokenRole.Comment, Style.Python.CommentLine, Style.Python.CommentBlock);
        Map(s, t, TokenRole.Number, Style.Python.Number);
        Map(s, t, TokenRole.Keyword, Style.Python.Word);
        Map(s, t, TokenRole.Keyword2, Style.Python.Word2, Style.Python.ClassName, Style.Python.DefName);
        Map(s, t, TokenRole.String, Style.Python.String, Style.Python.Character, Style.Python.Triple, Style.Python.TripleDouble, Style.Python.StringEol);
        Map(s, t, TokenRole.Operator, Style.Python.Operator);
        Map(s, t, TokenRole.Preprocessor, Style.Python.Decorator);
        s.SetKeywords(0, PythonKeywords);
        s.SetKeywords(1, PythonBuiltins);
    }

    private static void Markup(Scintilla s, Theme t, string lexer, string tags)
    {
        s.LexerName = lexer;
        Map(s, t, TokenRole.Tag, Style.Html.Tag, Style.Html.TagUnknown, Style.Html.TagEnd);
        Map(s, t, TokenRole.Attribute, Style.Html.Attribute, Style.Html.AttributeUnknown);
        Map(s, t, TokenRole.String, Style.Html.DoubleString, Style.Html.SingleString, Style.Html.CData);
        Map(s, t, TokenRole.Number, Style.Html.Number, Style.Html.Entity);
        Map(s, t, TokenRole.Comment, Style.Html.Comment);
        Map(s, t, TokenRole.Preprocessor, Style.Html.XmlStart, Style.Html.XmlEnd);
        if (tags.Length > 0) s.SetKeywords(0, tags);
    }

    private static void Css(Scintilla s, Theme t)
    {
        s.LexerName = "css";
        Map(s, t, TokenRole.Comment, Style.Css.Comment);
        Map(s, t, TokenRole.Tag, Style.Css.Tag);
        Map(s, t, TokenRole.Keyword2, Style.Css.Class, Style.Css.Id);
        Map(s, t, TokenRole.Attribute, Style.Css.Identifier, Style.Css.UnknownIdentifier);
        Map(s, t, TokenRole.String, Style.Css.Value, Style.Css.DoubleString, Style.Css.SingleString);
        Map(s, t, TokenRole.Operator, Style.Css.Operator);
        Map(s, t, TokenRole.Preprocessor, Style.Css.Important, Style.Css.Directive);
    }

    private static void Json(Scintilla s, Theme t)
    {
        s.LexerName = "json";
        Map(s, t, TokenRole.Comment, Style.Json.LineComment, Style.Json.BlockComment);
        Map(s, t, TokenRole.Number, Style.Json.Number);
        Map(s, t, TokenRole.String, Style.Json.String, Style.Json.StringEol, Style.Json.Uri);
        Map(s, t, TokenRole.Attribute, Style.Json.PropertyName);
        Map(s, t, TokenRole.Preprocessor, Style.Json.EscapeSequence);
        Map(s, t, TokenRole.Keyword, Style.Json.Keyword);
        Map(s, t, TokenRole.Operator, Style.Json.Operator);
        s.SetKeywords(0, "true false null");
    }

    private static void Markdown(Scintilla s, Theme t)
    {
        s.LexerName = "markdown";
        Map(s, t, TokenRole.Heading, Style.Markdown.Header1, Style.Markdown.Header2, Style.Markdown.Header3, Style.Markdown.Header4, Style.Markdown.Header5, Style.Markdown.Header6);
        Map(s, t, TokenRole.Keyword2, Style.Markdown.Strong1, Style.Markdown.Strong2, Style.Markdown.Em1, Style.Markdown.Em2);
        Map(s, t, TokenRole.String, Style.Markdown.Code, Style.Markdown.Code2, Style.Markdown.CodeBk);
        Map(s, t, TokenRole.Attribute, Style.Markdown.Link);
        Map(s, t, TokenRole.Comment, Style.Markdown.BlockQuote, Style.Markdown.HRule);
        Map(s, t, TokenRole.Operator, Style.Markdown.UListItem, Style.Markdown.OListItem);
        s.Styles[Style.Markdown.Header1].Bold = true;
        s.Styles[Style.Markdown.Header2].Bold = true;
    }
}
