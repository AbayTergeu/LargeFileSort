using System.Text;

namespace FileGenerator.Tests;

public class RandomLinesWriterTests
{
    // "1000000. " + four longest words with spaces + "\n" is well below this.
    private const int MaxLineLength = 64;

    [Theory]
    [InlineData(100L)]
    [InlineData(10_000L)]
    [InlineData(1_000_000L)]
    public void Write_SizeIsWithinOneLineOfTarget(long targetSize)
    {
        var content = Write(targetSize);

        var size = Encoding.UTF8.GetByteCount(content);
        Assert.True(size <= targetSize, $"Size {size} is larger than target {targetSize}.");
        Assert.True(targetSize - size < MaxLineLength, $"Size {size} is too far from target {targetSize}.");
    }

    [Fact]
    public void Write_EveryLineHasNumberDotSpaceText()
    {
        var content = Write(100_000);

        Assert.EndsWith("\n", content);
        foreach (var line in content.TrimEnd('\n').Split('\n'))
        {
            Assert.Matches(@"^[1-9][0-9]*\. [A-Z][a-z ]*$", line);
        }
    }

    [Fact]
    public void Write_RepeatsTextParts()
    {
        var lines = Write(100_000).TrimEnd('\n').Split('\n');

        var texts = lines.Select(line => line[(line.IndexOf(". ", StringComparison.Ordinal) + 2)..]).ToList();

        Assert.True(texts.Distinct().Count() < texts.Count, "Expected some lines to share the same text.");
    }

    [Fact]
    public void Write_SameSeed_ProducesSameContent()
    {
        Assert.Equal(Write(10_000, seed: 42), Write(10_000, seed: 42));
    }

    [Fact]
    public void Write_DifferentSeed_ProducesDifferentContent()
    {
        Assert.NotEqual(Write(10_000, seed: 1), Write(10_000, seed: 2));
    }

    [Fact]
    public void Write_TargetSmallerThanOneLine_Throws()
    {
        var writer = new RandomLinesWriter(seed: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Write(new MemoryStream(), targetSize: 1));
    }

    private static string Write(long targetSize, int seed = 1)
    {
        using var output = new MemoryStream();
        new RandomLinesWriter(seed).Write(output, targetSize);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
