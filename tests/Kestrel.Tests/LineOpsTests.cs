using Kestrel.Core.Text;

namespace Kestrel.Tests;

public class LineOpsTests
{
    [Fact]
    public void Sorts_ordinal_by_default() =>
        Assert.Equal(new[] { "B", "a", "c" }, LineOps.SortAscending(new[] { "c", "a", "B" }));

    [Fact]
    public void Sorts_ignoring_case() =>
        Assert.Equal(new[] { "a", "B", "c" }, LineOps.SortAscending(new[] { "c", "a", "B" }, ignoreCase: true));

    [Fact]
    public void Sorts_descending() =>
        Assert.Equal(new[] { "c", "b", "a" }, LineOps.SortDescending(new[] { "b", "c", "a" }));

    [Fact]
    public void Removes_duplicates_keeping_first_order() =>
        Assert.Equal(new[] { "x", "y", "z" }, LineOps.RemoveDuplicates(new[] { "x", "y", "x", "z", "y" }));

    [Fact]
    public void Removes_empty_and_whitespace_lines() =>
        Assert.Equal(new[] { "a", "b" }, LineOps.RemoveEmpty(new[] { "a", "", "  ", "\t", "b" }));

    [Fact]
    public void Trims_trailing_whitespace_only() =>
        Assert.Equal(new[] { "a", "b", " c" }, LineOps.TrimTrailing(new[] { "a  ", "b\t", " c " }));

    [Fact]
    public void Split_handles_mixed_line_endings() =>
        Assert.Equal(new[] { "a", "b", "c", "d" }, LineOps.Split("a\r\nb\nc\rd"));

    [Fact]
    public void Join_uses_requested_line_ending() =>
        Assert.Equal("a\nb", LineOps.Join(new[] { "a", "b" }, EolMode.Lf));
}
