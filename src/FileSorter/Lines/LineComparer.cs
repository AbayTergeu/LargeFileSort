namespace FileSorter.Lines;

internal static class LineComparer
{
    public static int Compare(
        ReadOnlySpan<byte> x, ParsedLine xParsed,
        ReadOnlySpan<byte> y, ParsedLine yParsed)
    {
        int byText = x[xParsed.TextStart..].SequenceCompareTo(y[yParsed.TextStart..]);
        if (byText != 0)
        {
            return byText;
        }

        int byNumber = xParsed.Number.CompareTo(yParsed.Number);
        if (byNumber != 0)
        {
            return byNumber;
        }

        return x.SequenceCompareTo(y);
    }
}
