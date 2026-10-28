using System.Globalization;

namespace FileSorter.Lines;

internal static class LineParser
{
    public static bool TryParse(ReadOnlySpan<byte> line, out ParsedLine result)
    {
        result = default;

        int dot = line.IndexOf((byte)'.');
        if (dot < 0 || dot + 1 >= line.Length || line[dot + 1] != (byte)' ')
        {
            return false;
        }

        if (!long.TryParse(line[..dot], NumberStyles.None, CultureInfo.InvariantCulture, out long number))
        {
            return false;
        }

        result = new ParsedLine(number, dot + 2);
        return true;
    }
}
