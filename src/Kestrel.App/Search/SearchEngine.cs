using System.Text.RegularExpressions;
using ScintillaNET;

namespace Kestrel.App.Search;

internal sealed record SearchOptions(string Pattern, string Replacement, bool MatchCase, bool WholeWord, bool UseRegex, bool WrapAround);

/// <summary>Find/replace on Scintilla's target API. Regex mode uses Scintilla's C++11 (ECMAScript) engine.</summary>
internal static class SearchEngine
{
    private const int MaxOperations = 1_000_000;

    /// <summary>Returns an error message, or null when the options are usable.</summary>
    public static string? Validate(SearchOptions o)
    {
        if (string.IsNullOrEmpty(o.Pattern)) return "Enter text to find.";
        if (!o.UseRegex) return null;
        try
        {
            _ = new Regex(o.Pattern, RegexOptions.ECMAScript);
            return null;
        }
        catch (ArgumentException ex)
        {
            return "Invalid regular expression: " + ex.Message;
        }
    }

    public static bool FindNext(Scintilla s, SearchOptions o, bool forward)
    {
        Prepare(s, o);
        int length = s.TextLength;
        int pos;
        if (forward)
        {
            int start = s.SelectionEnd;
            pos = Search(s, start, length, o.Pattern);
            // Step past an empty match at the caret so patterns like ^ can advance.
            if (pos == start && s.TargetEnd == s.TargetStart && start < length) pos = Search(s, start + 1, length, o.Pattern);
            if (pos < 0 && o.WrapAround && start > 0) pos = Search(s, 0, start, o.Pattern);
        }
        else
        {
            int start = s.SelectionStart;
            pos = Search(s, start, 0, o.Pattern);
            if (pos < 0 && o.WrapAround && start < length) pos = Search(s, length, start, o.Pattern);
        }
        if (pos < 0) return false;
        s.SetSelection(s.TargetEnd, s.TargetStart);
        s.ScrollCaret();
        return true;
    }

    /// <summary>Replaces the selection if it is a match, then moves to the next match.</summary>
    public static bool ReplaceCurrent(Scintilla s, SearchOptions o)
    {
        Prepare(s, o);
        int selStart = s.SelectionStart;
        int selEnd = s.SelectionEnd;
        if (selEnd > selStart && Search(s, selStart, selEnd, o.Pattern) == selStart && s.TargetEnd == selEnd)
        {
            ReplaceTarget(s, o);
            s.SetSelection(s.TargetEnd, s.TargetEnd);
        }
        return FindNext(s, o, forward: true);
    }

    public static int ReplaceAll(Scintilla s, SearchOptions o)
    {
        Prepare(s, o);
        int count = 0;
        int pos = 0;
        s.BeginUndoAction();
        try
        {
            while (count < MaxOperations && pos <= s.TextLength)
            {
                if (Search(s, pos, s.TextLength, o.Pattern) < 0) break;
                bool empty = s.TargetEnd == s.TargetStart;
                ReplaceTarget(s, o);
                count++;
                pos = s.TargetEnd + (empty ? 1 : 0);
            }
        }
        finally
        {
            s.EndUndoAction();
        }
        return count;
    }

    public static int Count(Scintilla s, SearchOptions o)
    {
        Prepare(s, o);
        int count = 0;
        int pos = 0;
        while (count < MaxOperations && pos <= s.TextLength)
        {
            if (Search(s, pos, s.TextLength, o.Pattern) < 0) break;
            count++;
            pos = s.TargetEnd + (s.TargetEnd == s.TargetStart ? 1 : 0);
        }
        return count;
    }

    private static void Prepare(Scintilla s, SearchOptions o)
    {
        var flags = SearchFlags.None;
        if (o.MatchCase) flags |= SearchFlags.MatchCase;
        if (o.WholeWord && !o.UseRegex) flags |= SearchFlags.WholeWord;
        if (o.UseRegex) flags |= SearchFlags.Regex | SearchFlags.Cxx11Regex;
        s.SearchFlags = flags;
    }

    private static int Search(Scintilla s, int from, int to, string pattern)
    {
        s.TargetStart = from;
        s.TargetEnd = to;
        return s.SearchInTarget(pattern);
    }

    private static void ReplaceTarget(Scintilla s, SearchOptions o)
    {
        // ReplaceTargetRe expands $1 / \\1 group references from the last regex match.
        if (o.UseRegex) s.ReplaceTargetRe(o.Replacement);
        else s.ReplaceTarget(o.Replacement);
    }
}
