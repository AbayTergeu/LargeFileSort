using System.Globalization;

namespace FileSorter.Features.Sort;

internal sealed record SortOptions(
    string InputPath,
    string OutputPath,
    string TempParentPath,
    int ChunkSize,
    int MergeFanIn,
    int Workers,
    bool SkipInvalid)
{
    public const int DefaultChunkMegabytes = 64;
    public const int DefaultMergeFanIn = 64;
    public const int DefaultMaxWorkers = 4;

    private const int MaxChunkMegabytes = 2047;
    private const int MaxWorkers = 64;
    // Each merged run is an open file; operating systems limit open files per process (often to 1024).
    private const int MaxMergeFanIn = 512;

    public static string Usage =>
        """
        Usage: FileSorter sort <input> [options]

          --output <path>     Result file. Default: <input name>.sorted<extension> next to the input.
          --temp-dir <path>   Folder for temporary files. Default: the folder of the output file.
          --chunk-mb <n>      Size of one in-memory chunk, 1..2047. Default: 64.
          --workers <n>       How many chunks are sorted in parallel. Default: CPU cores, at most 4.
          --fan-in <n>        How many runs are merged at once, 2..512. Default: 64.
          --skip-invalid      Skip lines that are not "<number>. <text>" instead of stopping.
        """;

    public static bool TryParse(string[] args, out SortOptions? options, out string? error)
    {
        options = null;
        error = null;

        if (args.Length == 0 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            error = "The input file is required.";
            return false;
        }

        var inputPath = Path.GetFullPath(args[0]);
        string? outputPath = null;
        string? tempParentPath = null;
        var chunkMegabytes = DefaultChunkMegabytes;
        var fanIn = DefaultMergeFanIn;
        // More workers did not make sorting faster in measurements (see README), but each one adds a chunk of memory.
        var workers = Math.Min(Environment.ProcessorCount, DefaultMaxWorkers);
        var skipInvalid = false;

        for (var i = 1; i < args.Length; i++)
        {
            var name = args[i];
            if (name == "--skip-invalid")
            {
                skipInvalid = true;
                continue;
            }

            if (i + 1 >= args.Length)
            {
                error = $"Missing value for '{name}'.";
                return false;
            }

            var value = args[++i];
            switch (name)
            {
                case "--output":
                    outputPath = Path.GetFullPath(value);
                    break;
                case "--temp-dir":
                    tempParentPath = Path.GetFullPath(value);
                    break;
                case "--chunk-mb" when TryParseInt(value, 1, MaxChunkMegabytes, out chunkMegabytes):
                    break;
                case "--fan-in" when TryParseInt(value, 2, MaxMergeFanIn, out fanIn):
                    break;
                case "--workers" when TryParseInt(value, 1, MaxWorkers, out workers):
                    break;
                case "--chunk-mb" or "--fan-in" or "--workers":
                    error = $"Invalid value for '{name}': '{value}'.";
                    return false;
                default:
                    error = $"Unknown option '{name}'.";
                    return false;
            }
        }

        outputPath ??= DefaultOutputPath(inputPath);
        if (string.Equals(inputPath, outputPath, PathComparison))
        {
            error = "The output file must be different from the input file.";
            return false;
        }

        options = new SortOptions(
            inputPath,
            outputPath,
            tempParentPath ?? Path.GetDirectoryName(outputPath)!,
            chunkMegabytes * 1024 * 1024,
            fanIn,
            workers,
            skipInvalid);
        return true;
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static string DefaultOutputPath(string inputPath) =>
        Path.Combine(
            Path.GetDirectoryName(inputPath)!,
            Path.GetFileNameWithoutExtension(inputPath) + ".sorted" + Path.GetExtension(inputPath));

    private static bool TryParseInt(string text, int min, int max, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value)
        && value >= min
        && value <= max;
}
