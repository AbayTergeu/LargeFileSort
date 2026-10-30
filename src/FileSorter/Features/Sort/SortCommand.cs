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

        SplitPhase split;
        List<string> runs;
        using (var input = new FileStream(options.InputPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0, FileOptions.SequentialScan))
        {
            // A small input does not need full-size chunk buffers.
            // The extra byte lets the reader reach the end of the input instead of seeing a full buffer.
            var chunkSize = (int)Math.Min(options.ChunkSize, input.Length + 1);
            split = new SplitPhase(options with { ChunkSize = chunkSize }, tempDirectory);
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
