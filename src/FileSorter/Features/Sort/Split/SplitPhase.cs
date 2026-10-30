using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using FileSorter.Lines;

namespace FileSorter.Features.Sort.Split;

/// <summary>
/// Phase 1: cuts the input into chunks that fit in memory, sorts each chunk
/// and writes it to a temporary "run" file.
/// One thread reads chunks; up to <see cref="SortOptions.Workers"/> threads sort and write them in parallel.
/// </summary>
internal sealed class SplitPhase(SortOptions options, TempDirectory tempDirectory)
{
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private long _lineCount;
    private long _skippedLineCount;

    public long LineCount => _lineCount;

    public long SkippedLineCount => _skippedLineCount;

    public List<string> Run(Stream input, CancellationToken cancellationToken)
    {
        // The reader fills one workspace while every worker sorts another.
        using var pool = new ChunkPool(options.Workers + 1, options.ChunkSize);
        var runs = new ConcurrentBag<string>();

        // NoBuffering: a worker takes one chunk at a time, so no extra chunks are held in a queue.
        var chunks = Partitioner.Create(ReadChunks(input, pool), EnumerablePartitionerOptions.NoBuffering);
        try
        {
            Parallel.ForEach(
                chunks,
                new ParallelOptions { MaxDegreeOfParallelism = options.Workers, CancellationToken = cancellationToken },
                chunk =>
                {
                    try
                    {
                        if (SortChunk(chunk) is { } run)
                        {
                            runs.Add(run);
                        }
                    }
                    finally
                    {
                        pool.Return(chunk.Workspace);
                    }
                });
        }
        catch (AggregateException e)
        {
            // Several chunks can fail at the same time; report the earliest invalid line.
            var first = e.InnerExceptions.OfType<InvalidLineException>().MinBy(x => x.LineNumber) ?? e.InnerExceptions[0];
            ExceptionDispatchInfo.Throw(first);
        }

        return [.. runs];
    }

    private static IEnumerable<Chunk> ReadChunks(Stream input, ChunkPool pool)
    {
        var reader = new ChunkReader(input);
        long firstLineNumber = 1;
        var isFirstChunk = true;

        while (true)
        {
            var workspace = pool.Rent();
            var length = reader.ReadChunk(workspace.Buffer);
            if (length == 0)
            {
                pool.Return(workspace);
                yield break;
            }

            var start = isFirstChunk && workspace.Buffer.AsSpan(0, length).StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
            isFirstChunk = false;

            var chunk = new Chunk(workspace, start, length, firstLineNumber);
            firstLineNumber += CountLines(workspace.Buffer.AsSpan(start, length - start));
            yield return chunk;
        }
    }

    private static long CountLines(ReadOnlySpan<byte> chunk) =>
        chunk.IsEmpty ? 0 : chunk.Count((byte)'\n') + (chunk[^1] == (byte)'\n' ? 0 : 1);

    /// <summary>
    /// Parses, sorts and writes one chunk. Returns the run path, or null if the chunk has no valid lines.
    /// </summary>
    private string? SortChunk(Chunk chunk)
    {
        var lines = chunk.Workspace.Lines;
        ParseLines(chunk, lines);
        if (lines.Count == 0)
        {
            return null;
        }

        var buffer = chunk.Workspace.Buffer;
        CollectionsMarshal.AsSpan(lines).Sort((x, y) => LineComparer.Compare(
            buffer.AsSpan(x.Offset, x.Length), x.Parsed,
            buffer.AsSpan(y.Offset, y.Length), y.Parsed));

        return WriteRun(buffer, lines);
    }

    private void ParseLines(Chunk chunk, List<LineRef> lines)
    {
        lines.Clear();
        var lineNumber = chunk.FirstLineNumber;
        var skipped = 0;

        var start = chunk.Start;
        while (start < chunk.End)
        {
            var remaining = chunk.Workspace.Buffer.AsSpan(start, chunk.End - start);
            var lineBreak = remaining.IndexOf((byte)'\n');
            var line = lineBreak < 0 ? remaining : remaining[..lineBreak];
            if (line.EndsWith((byte)'\r'))
            {
                line = line[..^1];
            }

            if (LineParser.TryParse(line, out var parsed))
            {
                lines.Add(new LineRef(start, line.Length, parsed));
            }
            else if (options.SkipInvalid)
            {
                skipped++;
            }
            else
            {
                throw new InvalidLineException(lineNumber, line);
            }

            lineNumber++;
            start += lineBreak < 0 ? remaining.Length : lineBreak + 1;
        }

        Interlocked.Add(ref _lineCount, lines.Count);
        Interlocked.Add(ref _skippedLineCount, skipped);
    }

    private string WriteRun(byte[] buffer, List<LineRef> lines)
    {
        var path = tempDirectory.NewFilePath();
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1 << 20);
        foreach (var line in lines)
        {
            output.Write(buffer, line.Offset, line.Length);
            output.WriteByte((byte)'\n');
        }

        return path;
    }

    /// <summary>
    /// A part of the input in the workspace buffer, from <see cref="Start"/> to <see cref="End"/>.
    /// </summary>
    private readonly record struct Chunk(ChunkWorkspace Workspace, int Start, int End, long FirstLineNumber);
}
