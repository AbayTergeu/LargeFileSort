using System.Text;
using FileSorter.Features.Sort;
using FileSorter.Features.Sort.Merge;
using FileSorter.Tests.TestSupport;

namespace FileSorter.Tests.Features.Sort.Merge;

public sealed class MergePhaseTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly TempDirectory _tempDirectory;

    public MergePhaseTests()
    {
        _tempDirectory = new TempDirectory(_folder.Path);
    }

    public void Dispose()
    {
        _tempDirectory.Dispose();
        _folder.Dispose();
    }

    [Fact]
    public void Run_MergesSortedRuns()
    {
        var output = Merge(fanIn: 64, "1. A\n3. C\n", "2. B\n4. D\n");

        Assert.Equal("1. A\n2. B\n3. C\n4. D\n", output);
    }

    [Fact]
    public void Run_KeepsEqualLinesFromDifferentRuns()
    {
        var output = Merge(fanIn: 64, "1. Apple\n", "1. Apple\n2. Apple\n", "1. Apple\n");

        Assert.Equal("1. Apple\n1. Apple\n1. Apple\n2. Apple\n", output);
    }

    [Fact]
    public void Run_EmptyRun_IsIgnored()
    {
        var output = Merge(fanIn: 64, "", "1. A\n", "");

        Assert.Equal("1. A\n", output);
    }

    [Fact]
    public void Run_NoRuns_WritesNothing()
    {
        Assert.Equal("", Merge(fanIn: 64));
    }

    [Fact]
    public void Run_LineLongerThanReadBuffer()
    {
        var longLine = "1. " + new string('x', 200_000) + "\n";

        var output = Merge(fanIn: 64, longLine, "2. A\n");

        Assert.Equal("2. A\n" + longLine, output);
    }

    [Fact]
    public void Run_MoreRunsThanFanIn_MergesInSeveralPassesAndDeletesMergedRuns()
    {
        var contents = Enumerable.Range(1, 10).Select(i => $"{i}. Line\n").Reverse().ToArray();
        var runs = WriteRuns(contents);

        var output = Merge(fanIn: 3, runs);

        Assert.Equal(string.Concat(Enumerable.Range(1, 10).Select(i => $"{i}. Line\n")), output);
        Assert.All(runs, run => Assert.False(File.Exists(run)));
    }

    private string Merge(int fanIn, params string[] runContents) => Merge(fanIn, WriteRuns(runContents));

    private string Merge(int fanIn, List<string> runs)
    {
        var options = new SortOptions("input.txt", "output.txt", _folder.Path, ChunkSize: 1024, fanIn, SkipInvalid: false);
        using var output = new MemoryStream();
        new MergePhase(options, _tempDirectory).Run(runs, output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private List<string> WriteRuns(string[] contents) => contents.Select(content =>
    {
        var path = _tempDirectory.NewFilePath();
        File.WriteAllText(path, content);
        return path;
    }).ToList();
}
