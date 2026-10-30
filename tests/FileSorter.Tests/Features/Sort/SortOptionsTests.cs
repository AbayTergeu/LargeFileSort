using FileSorter.Features.Sort;

namespace FileSorter.Tests.Features.Sort;

public class SortOptionsTests
{
    [Fact]
    public void TryParse_OnlyInput_UsesDefaults()
    {
        var input = Path.Combine(Path.GetTempPath(), "data.txt");

        Assert.True(SortOptions.TryParse([input], out var options, out _));

        Assert.Equal(Path.Combine(Path.GetTempPath(), "data.sorted.txt"), options!.OutputPath);
        Assert.Equal(Path.GetDirectoryName(options.OutputPath), options.TempParentPath);
        Assert.Equal(SortOptions.DefaultChunkMegabytes * 1024 * 1024, options.ChunkSize);
        Assert.Equal(SortOptions.DefaultMergeFanIn, options.MergeFanIn);
        Assert.Equal(Math.Min(Environment.ProcessorCount, SortOptions.DefaultMaxWorkers), options.Workers);
        Assert.False(options.SkipInvalid);
    }

    [Fact]
    public void TryParse_AllOptions()
    {
        string[] args = ["in.txt", "--output", "out.txt", "--temp-dir", "tmp", "--chunk-mb", "16", "--fan-in", "8", "--workers", "3", "--skip-invalid"];

        Assert.True(SortOptions.TryParse(args, out var options, out _));

        Assert.Equal(Path.GetFullPath("out.txt"), options!.OutputPath);
        Assert.Equal(Path.GetFullPath("tmp"), options.TempParentPath);
        Assert.Equal(16 * 1024 * 1024, options.ChunkSize);
        Assert.Equal(8, options.MergeFanIn);
        Assert.Equal(3, options.Workers);
        Assert.True(options.SkipInvalid);
    }

    [Fact]
    public void TryParse_OutputIsInputWrittenDifferently_Fails()
    {
        var input = Path.Combine(Path.GetTempPath(), "data.txt");
        var sameFile = Path.Combine(Path.GetTempPath(), ".", "data.txt");

        Assert.False(SortOptions.TryParse([input, "--output", sameFile], out _, out var error));
        Assert.Contains("different", error);
    }

    [Theory]
    [InlineData]
    [InlineData("--output", "out.txt")]
    [InlineData("in.txt", "--output")]
    [InlineData("in.txt", "--unknown", "1")]
    [InlineData("in.txt", "--chunk-mb", "0")]
    [InlineData("in.txt", "--chunk-mb", "2048")]
    [InlineData("in.txt", "--chunk-mb", "abc")]
    [InlineData("in.txt", "--fan-in", "1")]
    [InlineData("in.txt", "--fan-in", "513")]
    [InlineData("in.txt", "--workers", "0")]
    [InlineData("in.txt", "--workers", "65")]
    public void TryParse_InvalidArguments_Fails(params string[] args)
    {
        Assert.False(SortOptions.TryParse(args, out _, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }
}
