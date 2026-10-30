using System.Text;
using FileSorter.Lines;

namespace FileSorter.Tests.Lines;

public class LineParserTests
{
    [Theory]
    [InlineData("1. Apple", 1, "Apple")]
    [InlineData("1. Mr. Smith", 1, "Mr. Smith")]
    [InlineData("415. Apple", 415, "Apple")]
    [InlineData("5. ", 5, "")]
    [InlineData("01. Apple", 1, "Apple")]
    [InlineData("1.  Apple", 1, " Apple")]
    [InlineData("1. Яблоко", 1, "Яблоко")]
    public void TryParse_ValidLine_ReturnsNumberAndText(string line, long expectedNumber, string expectedText)
    {
        var bytes = Encoding.UTF8.GetBytes(line);

        var ok = LineParser.TryParse(bytes, out var result);

        Assert.True(ok);
        Assert.Equal(expectedNumber, result.Number);
        Assert.Equal(expectedText, Encoding.UTF8.GetString(bytes[result.TextStart..]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Apple")]
    [InlineData("1.Apple")]
    [InlineData("1.")]
    [InlineData(". Apple")]
    [InlineData("abc. Apple")]
    [InlineData("-5. Apple")]
    [InlineData("+5. Apple")]
    [InlineData(" 1. Apple")]
    [InlineData("1 . Apple")]
    [InlineData("1.5. Apple")]
    [InlineData("9223372036854775808. X")]
    public void TryParse_InvalidLine_ReturnsFalse(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line);

        var ok = LineParser.TryParse(bytes, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryParse_MaxLongNumber_ReturnsNumber()
    {
        var bytes = Encoding.UTF8.GetBytes("9223372036854775807. X");

        var ok = LineParser.TryParse(bytes, out var result);

        Assert.True(ok);
        Assert.Equal(long.MaxValue, result.Number);
        Assert.Equal("X", Encoding.UTF8.GetString(bytes[result.TextStart..]));
    }
}
