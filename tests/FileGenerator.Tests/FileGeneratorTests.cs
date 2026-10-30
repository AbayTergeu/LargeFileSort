using System.Text;

namespace FileGenerator.Tests;

public class FileGeneratorTests
{
    // "1000000. " + four longest words with spaces + "\n" is well below this.
    private const int MaxLineLength = 64;

    [Theory]
    [InlineData(100L)]
    [InlineData(10_000L)]
    [InlineData(1_000_000L)]
    public void Generate_SizeIsWithinOneLineOfTarget(long targetSize)
    {
        var content = Generate(targetSize);

        var size = Encoding.UTF8.GetByteCount(content);
        Assert.True(size <= targetSize, $"Size {size} is larger than target {targetSize}.");
        Assert.True(targetSize - size < MaxLineLength, $"Size {size} is too far from target {targetSize}.");
    }

    [Fact]
    public void Generate_EveryLineHasNumberDotSpaceText()
    {
        var content = Generate(100_000);

        Assert.EndsWith("\n", content);
        foreach (var line in content.TrimEnd('\n').Split('\n'))
        {
            Assert.Matches(@"^[1-9][0-9]*\. [A-Z][a-z ]*$", line);
        }
    }

    [Fact]
    public void Generate_RepeatsTextParts()
    {
        var lines = Generate(100_000).TrimEnd('\n').Split('\n');

        var texts = lines.Select(line => line[(line.IndexOf(". ", StringComparison.Ordinal) + 2)..]).ToList();

        Assert.True(texts.Distinct().Count() < texts.Count, "Expected some lines to share the same text.");
    }

    [Fact]
    public void Generate_SameSeed_ProducesSameContent()
    {
        Assert.Equal(Generate(10_000, seed: 42), Generate(10_000, seed: 42));
    }

    [Fact]
    public void Generate_DifferentSeed_ProducesDifferentContent()
    {
        Assert.NotEqual(Generate(10_000, seed: 1), Generate(10_000, seed: 2));
    }

    [Fact]
    public void Generate_TargetSmallerThanOneLine_Throws()
    {
        var generator = new RandomLinesWriter(seed: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(new MemoryStream(), targetSize: 1));
    }

    private static string Generate(long targetSize, int seed = 1)
    {
        using var output = new MemoryStream();
        new RandomLinesWriter(seed).Generate(output, targetSize);
        return Encoding.UTF8.GetString(output.ToArray());
    }
}
