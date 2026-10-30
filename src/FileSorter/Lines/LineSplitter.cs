namespace FileSorter.Lines;

internal static class LineSplitter
{
    /// <summary>
    /// Takes the first line of <paramref name="data"/>: everything up to '\n', without a trailing '\r'.
    /// The last line may have no '\n'. Returns how many bytes of <paramref name="data"/> were used.
    /// </summary>
    public static int NextLine(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> line)
    {
        var lineBreak = data.IndexOf((byte)'\n');
        line = lineBreak < 0 ? data : data[..lineBreak];
        if (line.EndsWith((byte)'\r'))
        {
            line = line[..^1];
        }

        return lineBreak < 0 ? data.Length : lineBreak + 1;
    }
}
