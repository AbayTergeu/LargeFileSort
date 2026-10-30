namespace FileSorter.Features.Sort;

internal sealed record SortOptions(
    string InputPath,
    string OutputPath,
    string TempParentPath,
    int ChunkSize,
    int MergeFanIn,
    bool SkipInvalid);
