namespace FileSorter.Features.Sort;

internal sealed class LineTooLongException(int chunkSize)
    : Exception($"A line is longer than the chunk size ({chunkSize:N0} bytes). Use a larger --chunk-mb.");
