using System.Text;
using FileSorter.Features.Sort;
using FileSorter.Features.Sort.Split;
using FileSorter.Lines;
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

        Assert.Equal(["1. A\n3. C\n", "2. B\n4. D\n"], runs.Order());
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
    public void Run_InvalidLinesInParallelChunks_ReportsTheEarliest()
    {
        // Every line is invalid, so several workers fail at the same time.
        var content = string.Concat(Enumerable.Repeat("invalid\n", 5000));
        var phase = CreatePhase(chunkSize: 16, skipInvalid: false, workers: 8);

        var exception = Assert.Throws<InvalidLineException>(() => phase.Run(Input(content), CancellationToken.None));

        Assert.Equal(1, exception.LineNumber);
    }

    [Fact]
    public void Run_InvalidLineInLaterChunk_HasLineNumberInWholeInput()
    {
        var lines = Enumerable.Range(1, 2000).Select(i => $"{i}. Line").ToArray();
        lines[1499] = "invalid";
        var phase = CreatePhase(chunkSize: 64, skipInvalid: false, workers: 4);

        var exception = Assert.Throws<InvalidLineException>(() => phase.Run(Input(string.Join('\n', lines)), CancellationToken.None));

        Assert.Equal(1500, exception.LineNumber);
    }

    [Fact]
    public void Run_SkipInvalid_CountsSkippedLines()
    {
        var phase = CreatePhase(chunkSize: 8, skipInvalid: true, workers: 4);

        var runs = phase.Run(Input("2. B\nApple\n\n1. A\n"), CancellationToken.None);

        Assert.Equal(["1. A\n", "2. B\n"], runs.Select(File.ReadAllText).Order());
        Assert.Equal(2, phase.LineCount);
        Assert.Equal(2, phase.SkippedLineCount);
    }

    private List<string> Split(string content, int chunkSize = 1024) =>
        CreatePhase(chunkSize, skipInvalid: false).Run(Input(content), CancellationToken.None).Select(File.ReadAllText).ToList();

    private SplitPhase CreatePhase(int chunkSize, bool skipInvalid, int workers = 1)
    {
        var options = new SortOptions("input.txt", "output.txt", _folder.Path, chunkSize, MergeFanIn: 2, workers, skipInvalid);
        return new SplitPhase(options, new TempDirectory(_folder.Path));
    }

    private static MemoryStream Input(string content) => new(Encoding.UTF8.GetBytes(content));
}
