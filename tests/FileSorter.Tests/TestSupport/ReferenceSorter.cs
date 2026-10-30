using System.Text;
using FileSorter.Lines;

namespace FileSorter.Tests.TestSupport;

/// <summary>
/// Sorts lines in memory with the same comparer, as the expected result for external sort tests.
/// </summary>
public static class ReferenceSorter
{
    public static List<string> Sort(IEnumerable<string> lines)
    {
        var sorted = lines.Select(Encoding.UTF8.GetBytes).ToList();
        sorted.Sort((x, y) =>
        {
            Assert.True(LineParser.TryParse(x, out var xParsed));
            Assert.True(LineParser.TryParse(y, out var yParsed));
            return LineComparer.Compare(x, xParsed, y, yParsed);
        });

        return sorted.Select(Encoding.UTF8.GetString).ToList();
    }
}
