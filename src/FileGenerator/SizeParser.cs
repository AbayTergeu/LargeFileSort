using System.Globalization;

namespace FileGenerator;

internal static class SizeParser
{
    private static readonly (string Suffix, long Multiplier)[] Units =
    [
        ("KB", 1024L),
        ("MB", 1024L * 1024),
        ("GB", 1024L * 1024 * 1024),
    ];

    public static bool TryParse(string text, out long bytes)
    {
        bytes = 0;

        var multiplier = 1L;
        var number = text;
        foreach (var (suffix, unitMultiplier) in Units)
        {
            if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                multiplier = unitMultiplier;
                number = text[..^suffix.Length];
                break;
            }
        }

        if (!long.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value == 0
            || value > long.MaxValue / multiplier)
        {
            return false;
        }

        bytes = value * multiplier;
        return true;
    }
}
