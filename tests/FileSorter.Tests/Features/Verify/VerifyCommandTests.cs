using System.Text;
using FileSorter.Features.Sort;
using FileSorter.Features.Verify;
using FileSorter.Lines;
using FileSorter.Tests.TestSupport;

namespace FileSorter.Tests.Features.Verify;

public sealed class VerifyCommandTests : IDisposable
{
    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Theory]
    [InlineData("", 0)]
    [InlineData("1. Apple\n", 1)]
    [InlineData("1. Apple\n415. Apple\n2. Banana is yellow\n", 3)]
    [InlineData("1. Apple\n1. Apple\n", 2)]
    [InlineData("1. A\r\n2. B", 2)]
    public void Run_SortedFile_IsSorted(string content, long expectedLineCount)
    {
        var result = Verify(content);

        Assert.True(result.IsSorted);
        Assert.Equal(expectedLineCount, result.LineCount);
    }

    [Theory]
    [InlineData("2. Banana\n1. Apple\n", 2)]
    [InlineData("1. Apple\n415. Apple\n2. Apple\n", 3)]
    [InlineData("1. Apple\n01. Apple\n", 2)]
    public void Run_UnsortedFile_ReportsFirstUnsortedLine(string content, long expectedLine)
    {
        var result = Verify(content);

        Assert.False(result.IsSorted);
        Assert.Equal(expectedLine, result.FirstUnsortedLine);
    }

    [Fact]
    public void Run_ComparesLinesAcrossChunkBorders()
    {
        var lines = Enumerable.Range(1, 200).Select(i => $"{i}. Line with a long enough text").ToList();
        lines.Insert(150, "1. Line with a long enough text");

        var result = Verify(string.Join('\n', lines), chunkSize: 64);

        Assert.Equal(151, result.FirstUnsortedLine);
    }

    [Fact]
    public void Run_InvalidLine_Throws()
    {
        var exception = Assert.Throws<InvalidLineException>(() => Verify("1. A\nApple\n"));

        Assert.Equal(2, exception.LineNumber);
    }

    [Fact]
    public void Run_SorterOutput_IsSorted()
    {
        var input = _folder.File("input.txt");
        File.WriteAllText(input, string.Join('\n', RandomLines.Create(seed: 8, count: 3000)));
        var options = new SortOptions(input, _folder.File("output.txt"), _folder.Path, ChunkSize: 256, MergeFanIn: 4, Workers: 4, SkipInvalid: false);
        SortCommand.Run(options, CancellationToken.None);

        var result = VerifyCommand.Run(options.OutputPath, CancellationToken.None, chunkSize: 128);

        Assert.True(result.IsSorted);
        Assert.Equal(3000, result.LineCount);
    }

    private VerifyResult Verify(string content, int chunkSize = 1024)
    {
        var path = _folder.File("input.txt");
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(content));
        return VerifyCommand.Run(path, CancellationToken.None, chunkSize);
    }
}
