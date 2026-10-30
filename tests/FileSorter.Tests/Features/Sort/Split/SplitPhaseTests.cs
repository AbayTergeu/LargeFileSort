using System.Text;
using FileSorter.Features.Sort;
using FileSorter.Features.Sort.Split;
using FileSorter.Tests.TestSupport;

namespace FileSorter.Tests.Features.Sort.Split;

public sealed class SplitPhaseTests : IDisposable
{
    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Run_WritesOneSortedRunPerChunk()
    {
        var runs = Split("3. C\n1. A\n4. D\n2. B\n", chunkSize: 10);

        Assert.Equal(["1. A\n3. C\n", "2. B\n4. D\n"], runs);
    }

    [Fact]
    public void Run_NormalizesLineEndingsAndAddsLastLineBreak()
    {
        var runs = Split("2. B\r\n1. A");

        Assert.Equal(["1. A\n2. B\n"], runs);
    }

    [Fact]
    public void Run_SkipsUtf8Bom()
    {
        var runs = Split("\uFEFF2. B\n1. A\n");

        Assert.Equal(["1. A\n2. B\n"], runs);
    }

    [Fact]
    public void Run_EmptyInput_WritesNoRuns()
    {
        Assert.Empty(Split(""));
    }

    [Fact]
    public void Run_InvalidLine_ThrowsWithLineNumber()
    {
        var exception = Assert.Throws<InvalidLineException>(() => Split("1. A\n2. B\nApple\n", chunkSize: 6));

        Assert.Equal(3, exception.LineNumber);
    }

    [Fact]
    public void Run_SkipInvalid_CountsSkippedLines()
    {
        var phase = CreatePhase(chunkSize: 1024, skipInvalid: true);

        var runs = phase.Run(Input("2. B\nApple\n\n1. A\n"));

        Assert.Equal(["1. A\n2. B\n"], runs.Select(File.ReadAllText));
        Assert.Equal(2, phase.LineCount);
        Assert.Equal(2, phase.SkippedLineCount);
    }

    private List<string> Split(string content, int chunkSize = 1024) =>
        CreatePhase(chunkSize, skipInvalid: false).Run(Input(content)).Select(File.ReadAllText).ToList();

    private SplitPhase CreatePhase(int chunkSize, bool skipInvalid)
    {
        var options = new SortOptions("input.txt", "output.txt", _folder.Path, chunkSize, MergeFanIn: 2, skipInvalid);
        return new SplitPhase(options, new TempDirectory(_folder.Path));
    }

    private static MemoryStream Input(string content) => new(Encoding.UTF8.GetBytes(content));
}
