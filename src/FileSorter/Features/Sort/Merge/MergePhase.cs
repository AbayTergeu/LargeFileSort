using FileSorter.Lines;

namespace FileSorter.Features.Sort.Merge;

/// <summary>
/// Phase 2: merges sorted runs into one sorted file.
/// At most <see cref="SortOptions.MergeFanIn"/> runs are open at once; when there are more,
/// they are merged in groups into larger runs first, and those are merged again.
/// </summary>
internal sealed class MergePhase(SortOptions options, TempDirectory tempDirectory)
{
    private const int ReadBufferSize = 64 * 1024;
    private const int WriteBufferSize = 1 << 20;

    public void Run(List<string> runs, Stream output)
    {
        while (runs.Count > options.MergeFanIn)
        {
            runs = MergeGroups(runs);
        }

        Merge(runs, output);
    }

    private List<string> MergeGroups(List<string> runs)
    {
        var merged = new List<string>();
        foreach (var group in runs.Chunk(options.MergeFanIn))
        {
            var path = tempDirectory.NewFilePath();
            using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, WriteBufferSize))
            {
                Merge(group, output);
            }

            foreach (var run in group)
            {
                File.Delete(run);
            }

            merged.Add(path);
        }

        return merged;
    }

    private static void Merge(IReadOnlyList<string> runs, Stream output)
    {
        var readers = new List<RunReader>(runs.Count);
        try
        {
            // Each reader is queued with itself as the priority: it is ordered by its current line.
            var queue = new PriorityQueue<RunReader, RunReader>(runs.Count, CurrentLineComparer.Instance);
            foreach (var run in runs)
            {
                var reader = new RunReader(run, ReadBufferSize);
                readers.Add(reader);
                if (reader.MoveNext())
                {
                    queue.Enqueue(reader, reader);
                }
            }

            while (queue.TryDequeue(out var reader, out _))
            {
                output.Write(reader.Current);
                output.WriteByte((byte)'\n');

                if (reader.MoveNext())
                {
                    queue.Enqueue(reader, reader);
                }
            }
        }
        finally
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
            }
        }
    }

    private sealed class CurrentLineComparer : IComparer<RunReader>
    {
        public static readonly CurrentLineComparer Instance = new();

        public int Compare(RunReader? x, RunReader? y) =>
            LineComparer.Compare(x!.Current, x.CurrentParsed, y!.Current, y.CurrentParsed);
    }
}
