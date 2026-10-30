using System.Globalization;

namespace FileSorter.Features.Sort;

internal sealed record SortOptions(
    string InputPath,
    string OutputPath,
    string TempParentPath,
    int ChunkSize,
    int MergeFanIn,
    bool SkipInvalid)
{
    public const int DefaultChunkMegabytes = 256;
    public const int DefaultMergeFanIn = 64;

    private const int MaxChunkMegabytes = 2047;

    public static string Usage =>
        """
        Usage: FileSorter sort <input> [options]

          --output <path>     Result file. Default: <input name>.sorted<extension> next to the input.
          --temp-dir <path>   Folder for temporary files. Default: the folder of the output file.
          --chunk-mb <n>      Size of one in-memory chunk, 1..2047. Default: 256.
          --fan-in <n>        How many runs are merged at once, 2 or more. Default: 64.
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
                case "--fan-in" when TryParseInt(value, 2, int.MaxValue, out fanIn):
                    break;
                case "--chunk-mb" or "--fan-in":
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
