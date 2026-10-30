using System.Text;
using FileSorter.Features.Sort;
using FileSorter.Lines;
using FileSorter.Tests.TestSupport;

namespace FileSorter.Tests.Features.Sort;

public sealed class SortCommandTests : IDisposable
{
    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Run_SortsExampleFromAssignment()
    {
        var output = Sort("415. Apple\n30432. Something something something\n1. Apple\n32. Cherry is the best\n2. Banana is yellow\n");

        Assert.Equal("1. Apple\n415. Apple\n2. Banana is yellow\n32. Cherry is the best\n30432. Something something something\n", output);
    }

    [Theory]
    [InlineData(1, 64, 2, 1)]
    [InlineData(2, 200, 3, 4)]
    [InlineData(3, 1000, 64, 8)]
    [InlineData(4, 50_000, 2, 2)]
    public void Run_MatchesInMemorySort(int seed, int chunkSize, int fanIn, int workers)
    {
        var lines = RandomLines.Create(seed, count: 2000);

        var output = Sort(string.Join('\n', lines) + "\n", chunkSize, fanIn, workers);

        var expected = string.Join('\n', ReferenceSorter.Sort(lines)) + "\n";
        Assert.True(expected == output, $"Output differs from the in-memory sort (seed {seed}).");
    }

    [Fact]
    public void Run_EmptyInput_CreatesEmptyOutput()
    {
        Assert.Equal("", Sort(""));
    }

    [Theory]
    [InlineData("1. A\nApple\n2. B\n", 2)]
    [InlineData("1. A\n\n2. B\n", 2)]
    [InlineData("1. A\n2. B\n3.C\n", 3)]
    public void Run_InvalidLine_ThrowsWithLineNumberAndLeavesNoFiles(string content, long expectedLineNumber)
    {
        var options = WriteInput(content, chunkSize: 6);

        var exception = Assert.Throws<InvalidLineException>(() => SortCommand.Run(options, CancellationToken.None));

        Assert.Equal(expectedLineNumber, exception.LineNumber);
        Assert.Equal(["input.txt"], Directory.GetFileSystemEntries(_folder.Path).Select(Path.GetFileName));
    }

    [Fact]
    public void Run_LineLongerThanChunk_ThrowsAndLeavesNoFiles()
    {
        var options = WriteInput("1. A\n2. This line is too long\n", chunkSize: 16);

        Assert.Throws<LineTooLongException>(() => SortCommand.Run(options, CancellationToken.None));

        Assert.Equal(["input.txt"], Directory.GetFileSystemEntries(_folder.Path).Select(Path.GetFileName));
    }

    [Fact]
    public void Run_SkipInvalid_SkipsAndCountsInvalidLines()
    {
        var options = WriteInput("2. B\nApple\n\n1. A\n-5. C\n") with { SkipInvalid = true };

        var result = SortCommand.Run(options, CancellationToken.None);

        Assert.Equal("1. A\n2. B\n", File.ReadAllText(options.OutputPath));
        Assert.Equal(2, result.LineCount);
        Assert.Equal(3, result.SkippedLineCount);
    }

    [Fact]
    public void Run_ExistingOutput_IsOverwritten()
    {
        var options = WriteInput("2. B\n1. A\n");
        File.WriteAllText(options.OutputPath, "old content that is longer than the new one\n");

        SortCommand.Run(options, CancellationToken.None);

        Assert.Equal("1. A\n2. B\n", File.ReadAllText(options.OutputPath));
    }

    [Fact]
    public void Run_RemovesTemporaryFiles()
    {
        var options = WriteInput(string.Join('\n', RandomLines.Create(seed: 5, count: 500)), chunkSize: 100, fanIn: 2);

        SortCommand.Run(options, CancellationToken.None);

        Assert.Equal(["input.txt", "output.txt"], Directory.GetFileSystemEntries(_folder.Path).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Run_Cancelled_ThrowsAndLeavesNoFiles()
    {
        var options = WriteInput(string.Join('\n', RandomLines.Create(seed: 7, count: 500)), chunkSize: 100);

        Assert.Throws<OperationCanceledException>(() => SortCommand.Run(options, new CancellationToken(canceled: true)));

        Assert.Equal(["input.txt"], Directory.GetFileSystemEntries(_folder.Path).Select(Path.GetFileName));
    }
    
    [Fact]
    public void Run_SameOutputForAnyNumberOfWorkers()
    {
        var content = string.Join('\n', RandomLines.Create(seed: 6, count: 5000));

        var outputs = new[] { 1, 2, 4, 8 }.Select(workers => Sort(content, chunkSize: 128, fanIn: 4, workers)).ToList();

        Assert.All(outputs, output => Assert.Equal(outputs[0], output));
    }

    private string Sort(string content, int chunkSize = 1024, int fanIn = 64, int workers = 1)
    {
        var options = WriteInput(content, chunkSize, fanIn, workers);
        SortCommand.Run(options, CancellationToken.None);
        return File.ReadAllText(options.OutputPath, Encoding.UTF8);
    }

    private SortOptions WriteInput(string content, int chunkSize = 1024, int fanIn = 64, int workers = 1)
    {
        var input = _folder.File("input.txt");
        File.WriteAllBytes(input, Encoding.UTF8.GetBytes(content));
        return new SortOptions(input, _folder.File("output.txt"), _folder.Path, chunkSize, fanIn, workers, SkipInvalid: false);
    }
}
