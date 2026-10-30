using System.Text;
using FileSorter.Lines;

namespace FileSorter.Tests.Lines;

public class LineComparerTests
{
    [Theory]
    [InlineData("1. Apple", "2. Banana", -1)]
    [InlineData("415. Apple", "1. Apple", 1)]
    [InlineData("1. Apple", "1. Apple", 0)]
    [InlineData("01. Apple", "1. Apple", -1)]
    [InlineData("1. Apple", "1. Apple pie", -1)]
    [InlineData("1. Zebra", "1. apple", -1)]
    [InlineData("2. Apple", "10. Apple", -1)]
    [InlineData("1. ", "1. A", -1)]
    [InlineData("1. Z", "1. Я", -1)]
    public void Compare_ReturnsExpectedOrder(string x, string y, int expectedSign)
    {
        Assert.Equal(expectedSign, Math.Sign(Compare(x, y)));
        Assert.Equal(-expectedSign, Math.Sign(Compare(y, x)));
    }

    [Fact]
    public void Compare_SortsExampleFromAssignment()
    {
        var lines = new List<string>
        {
            "415. Apple",
            "30432. Something something something",
            "1. Apple",
            "32. Cherry is the best",
            "2. Banana is yellow",
        };

        lines.Sort(Compare);

        string[] expected =
        [
            "1. Apple",
            "415. Apple",
            "2. Banana is yellow",
            "32. Cherry is the best",
            "30432. Something something something",
        ];
        Assert.Equal(expected, lines);
    }

    private static int Compare(string x, string y)
    {
        var xBytes = Encoding.UTF8.GetBytes(x);
        var yBytes = Encoding.UTF8.GetBytes(y);
        Assert.True(LineParser.TryParse(xBytes, out var xParsed));
        Assert.True(LineParser.TryParse(yBytes, out var yParsed));

        return LineComparer.Compare(xBytes, xParsed, yBytes, yParsed);
    }
}
