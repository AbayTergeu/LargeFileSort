namespace FileGenerator.Tests;

public class SizeParserTests
{
    [Theory]
    [InlineData("100", 100L)]
    [InlineData("1KB", 1024L)]
    [InlineData("10kb", 10L * 1024)]
    [InlineData("5MB", 5L * 1024 * 1024)]
    [InlineData("2GB", 2L * 1024 * 1024 * 1024)]
    public void TryParse_ValidSize_ReturnsBytes(string text, long expected)
    {
        Assert.True(SizeParser.TryParse(text, out var bytes));
        Assert.Equal(expected, bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("GB")]
    [InlineData("1.5GB")]
    [InlineData("10TB")]
    [InlineData(" 10MB")]
    [InlineData("99999999999GB")]
    public void TryParse_InvalidSize_ReturnsFalse(string text)
    {
        Assert.False(SizeParser.TryParse(text, out _));
    }
}
