using System.Text;

namespace FileGenerator;

/*
 * A fixed set of phrases. Lines pick their text from this set, so the same text
 * appears in many lines, as the assignment requires.
 */
internal sealed class PhrasePool
{
    private static readonly string[] Words =
    [
        "apple", "banana", "cherry", "is", "the", "best", "yellow", "something",
        "red", "green", "fresh", "sweet", "juicy", "big", "small", "orange",
        "lemon", "grape", "mango", "peach", "very", "not", "and", "or",
    ];

    private const int MaxWordsPerPhrase = 4;

    private readonly byte[][] _phrases;

    public PhrasePool(Random random, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        _phrases = new byte[size][];
        for (var i = 0; i < size; i++)
        {
            _phrases[i] = Encoding.UTF8.GetBytes(CreatePhrase(random));
        }

        MaxPhraseLength = _phrases.Max(phrase => phrase.Length);
    }

    public int MaxPhraseLength { get; }

    public ReadOnlySpan<byte> Next(Random random) => _phrases[random.Next(_phrases.Length)];

    private static string CreatePhrase(Random random)
    {
        var wordCount = random.Next(1, MaxWordsPerPhrase + 1);
        var words = new string[wordCount];
        for (var i = 0; i < wordCount; i++)
        {
            words[i] = Words[random.Next(Words.Length)];
        }

        var phrase = string.Join(' ', words);
        return char.ToUpperInvariant(phrase[0]) + phrase[1..];
    }
}
