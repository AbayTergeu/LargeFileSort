using FileSorter.Features.Sort.Merge;
using FileSorter.Features.Sort.Split;

namespace FileSorter.Features.Sort;

internal static class SortCommand
{
    private const int WriteBufferSize = 1 << 20;

    /// <summary>
    /// Sorts the input file into the output file. The result is first written to a partial file
    /// next to the output and renamed only at the end, so a failed sort never leaves a broken output.
    /// </summary>
    public static SortResult Run(SortOptions options, CancellationToken cancellationToken)
    {
        using var tempDirectory = new TempDirectory(options.TempParentPath);

        var split = new SplitPhase(options, tempDirectory);
        List<string> runs;
        using (var input = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0, FileOptions.SequentialScan))
        {
            runs = split.Run(input, cancellationToken);
        }

        var runCount = runs.Count;
        var partialOutput = options.OutputPath + ".partial";
        try
        {
            using (var output = new FileStream(partialOutput, FileMode.Create, FileAccess.Write, FileShare.None, WriteBufferSize))
            {
                new MergePhase(options, tempDirectory).Run(runs, output, cancellationToken);
            }

            File.Move(partialOutput, options.OutputPath, overwrite: true);
        }
        finally
        {
            File.Delete(partialOutput);
        }

        return new SortResult(split.LineCount, split.SkippedLineCount, runCount);
    }
}
