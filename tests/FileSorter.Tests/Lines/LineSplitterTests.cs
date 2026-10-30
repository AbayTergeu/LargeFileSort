using System.Text;
using FileSorter.Lines;

namespace FileSorter.Tests.Lines;

public class LineSplitterTests
{
    [Theory]
    [InlineData("1. A\n2. B\n", "1. A", 5)]
    [InlineData("1. A\r\n2. B\n", "1. A", 6)]
    [InlineData("1. A", "1. A", 4)]
    [InlineData("1. A\r", "1. A", 5)]
    [InlineData("\n2. B\n", "", 1)]
    public void NextLine_ReturnsLineWithoutLineEndAndConsumedBytes(string data, string expectedLine, int expectedConsumed)
    {
        var consumed = LineSplitter.NextLine(Encoding.UTF8.GetBytes(data), out var line);

        Assert.Equal(expectedLine, Encoding.UTF8.GetString(line));
        Assert.Equal(expectedConsumed, consumed);
    }
}
