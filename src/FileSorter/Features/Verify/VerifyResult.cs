namespace FileSorter.Features.Verify;

/// <param name="LineCount">Lines read; when the file is not sorted, up to the first unsorted line.</param>
/// <param name="FirstUnsortedLine">The first line that is smaller than the line before it, or null if the file is sorted.</param>
internal sealed record VerifyResult(long LineCount, long? FirstUnsortedLine)
{
    public bool IsSorted => FirstUnsortedLine is null;
}
