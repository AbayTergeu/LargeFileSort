using System.Diagnostics;
using FileSorter.Features.Sort;
using FileSorter.Lines;

if (args.Length == 0 || args[0] != "sort")
{
    Console.Error.WriteLine(SortOptions.Usage);
    return 1;
}

if (!SortOptions.TryParse(args[1..], out var options, out var error))
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine();
    Console.Error.WriteLine(SortOptions.Usage);
    return 1;
}

try
{
    var stopwatch = Stopwatch.StartNew();
    var result = SortCommand.Run(options!);

    Console.WriteLine($"Sorted {result.LineCount:N0} lines into '{options!.OutputPath}' in {stopwatch.Elapsed:hh\\:mm\\:ss\\.f}.");
    Console.WriteLine($"Runs: {result.RunCount:N0}. Peak memory: {Process.GetCurrentProcess().PeakWorkingSet64 / (1024 * 1024):N0} MB.");
    if (result.SkippedLineCount > 0)
    {
        Console.WriteLine($"Skipped {result.SkippedLineCount:N0} invalid lines.");
    }

    return 0;
}
catch (Exception e) when (e is InvalidLineException or LineTooLongException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 1;
}
