using System.Diagnostics;
using FileSorter.Features.Sort;
using FileSorter.Features.Verify;
using FileSorter.Lines;

const string usage =
    """
    Usage:
      FileSorter sort <input> [options]   Sort a file. Run "FileSorter sort" to see the options.
      FileSorter verify <file>            Check that a file is sorted.
    """;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    // Stop gracefully, so temporary files are deleted.
    e.Cancel = true;
    cancellation.Cancel();
};

try
{
    return args switch
    {
        ["sort", .. var sortArgs] => Sort(sortArgs, cancellation.Token),
        ["verify", var path] => Verify(path, cancellation.Token),
        _ => PrintUsage(),
    };
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Cancelled.");
    return 130;
}
catch (Exception e) when (e is InvalidLineException or LineTooLongException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 1;
}

static int PrintUsage()
{
    Console.Error.WriteLine(usage);
    return 1;
}

static int Sort(string[] args, CancellationToken cancellationToken)
{
    if (!SortOptions.TryParse(args, out var options, out var error))
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();
        Console.Error.WriteLine(SortOptions.Usage);
        return 1;
    }

    var stopwatch = Stopwatch.StartNew();
    var result = SortCommand.Run(options!, cancellationToken);

    Console.WriteLine($"Sorted {result.LineCount:N0} lines into '{options!.OutputPath}' in {stopwatch.Elapsed:hh\\:mm\\:ss\\.f}.");
    Console.WriteLine($"Runs: {result.RunCount:N0}. Peak memory: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N0} MB.");
    if (result.SkippedLineCount > 0)
    {
        Console.WriteLine($"Skipped {result.SkippedLineCount:N0} invalid lines.");
    }

    return 0;
}

static int Verify(string path, CancellationToken cancellationToken)
{
    var result = VerifyCommand.Run(path, cancellationToken);
    if (result.IsSorted)
    {
        Console.WriteLine($"Sorted: {result.LineCount:N0} lines.");
        return 0;
    }

    Console.Error.WriteLine($"Not sorted: line {result.FirstUnsortedLine:N0} is smaller than the line before it.");
    return 1;
}
