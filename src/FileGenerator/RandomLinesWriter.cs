using System.Globalization;

namespace FileGenerator;

internal sealed class RandomLinesWriter
{
    private const int PhrasePoolSize = 1000;
    private const long MaxNumber = 1_000_000;

    private readonly Random _random;
    private readonly PhrasePool _phrases;

    public RandomLinesWriter(int seed)
    {
        _random = new Random(seed);
        _phrases = new PhrasePool(_random, PhrasePoolSize);
    }

    /// <summary>
    /// Writes lines until the next line would exceed <paramref name="targetSize"/>.
    /// The result is never larger than the target and is smaller by less than one line.
    /// </summary>
    public void Generate(Stream output, long targetSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetSize);

        // number + ". " + phrase + "\n"
        var line = new byte[MaxNumber.ToString(CultureInfo.InvariantCulture).Length + 2 + _phrases.MaxPhraseLength + 1];
        long written = 0;

        while (true)
        {
            var length = WriteLine(line);
            if (written + length > targetSize)
            {
                break;
            }

            output.Write(line, 0, length);
            written += length;
        }

        if (written == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSize), targetSize,
                "Target size is too small for a single line.");
        }
    }
    
    private int WriteLine(Span<byte> line)
    {
        var number = _random.NextInt64(1, MaxNumber + 1);
        number.TryFormat(line, out var length, provider: CultureInfo.InvariantCulture);

        ". "u8.CopyTo(line[length..]);
        length += 2;

        var phrase = _phrases.Next(_random);
        phrase.CopyTo(line[length..]);
        length += phrase.Length;

        line[length] = (byte)'\n';
        return length + 1;
    }
}
