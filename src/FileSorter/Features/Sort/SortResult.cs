namespace FileSorter.Features.Sort;

internal sealed record SortResult(long LineCount, long SkippedLineCount, int RunCount);
