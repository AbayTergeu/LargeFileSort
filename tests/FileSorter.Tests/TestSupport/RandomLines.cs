namespace FileSorter.Tests.TestSupport;

/// <summary>
/// Random valid lines with cases the generator does not produce:
/// repeated texts, equal numbers, leading zeros, mixed case, Cyrillic, emoji and empty texts.
/// </summary>
public static class RandomLines
{
    private static readonly string[] Texts =
    [
        "Apple", "apple", "Apple pie", "Banana is yellow", "Cherry is the best", "Яблоко", "ёж",
        "Zebra", "😀 smile", "\uFFFD", "", " leading space", "Something something something",
    ];

    public static List<string> Create(int seed, int count)
    {
        var random = new Random(seed);
        var lines = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var number = random.Next(0, 50).ToString();
            if (random.Next(10) == 0)
            {
                number = "0" + number;
            }

            lines.Add($"{number}. {Texts[random.Next(Texts.Length)]}");
        }

        return lines;
    }
}
