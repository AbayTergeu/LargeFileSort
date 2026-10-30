using System.Text;

namespace FileSorter.Lines;

internal sealed class InvalidLineException(long lineNumber, ReadOnlySpan<byte> line)
    : Exception($"Line {lineNumber:N0} has an invalid format: \"{Preview(line)}\". Expected \"<number>. <text>\".")
{
    private const int MaxPreviewLength = 100;

    public long LineNumber { get; } = lineNumber;

    private static string Preview(ReadOnlySpan<byte> line)
    {
        return line.Length <= MaxPreviewLength
            ? Encoding.UTF8.GetString(line)
            : Encoding.UTF8.GetString(line[..MaxPreviewLength]) + "...";
    }
}
