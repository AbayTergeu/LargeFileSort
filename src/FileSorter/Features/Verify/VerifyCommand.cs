using FileSorter.Lines;

namespace FileSorter.Features.Verify;

/// <summary>
/// Checks that a file is sorted by the same rules as the sorter: every line is valid
/// and no line is greater than the next one. Reads the file once, in chunks.
/// </summary>
internal static class VerifyCommand
{
    public const int DefaultChunkSize = 16 * 1024 * 1024;

    /// <param name="chunkSize">How much of the file is read at once. Tests use a small value to cross chunk borders.</param>
    public static VerifyResult Run(string path, CancellationToken cancellationToken, int chunkSize = DefaultChunkSize)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0, FileOptions.SequentialScan);
        var reader = new ChunkReader(input);
        var buffer = new byte[chunkSize];

        // The previous line is copied out, because the next chunk overwrites the buffer.
        var previous = new byte[256];
        var previousLength = -1;
        ParsedLine previousParsed = default;
        long lineNumber = 0;

        for (var length = reader.ReadChunk(buffer); length > 0; length = reader.ReadChunk(buffer))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remaining = buffer.AsSpan(0, length);
            while (!remaining.IsEmpty)
            {
                var lineBreak = remaining.IndexOf((byte)'\n');
                var line = lineBreak < 0 ? remaining : remaining[..lineBreak];
                remaining = lineBreak < 0 ? [] : remaining[(lineBreak + 1)..];
                if (line.EndsWith((byte)'\r'))
                {
                    line = line[..^1];
                }

                lineNumber++;
                if (!LineParser.TryParse(line, out var parsed))
                {
                    throw new InvalidLineException(lineNumber, line);
                }

                if (previousLength >= 0 && LineComparer.Compare(previous.AsSpan(0, previousLength), previousParsed, line, parsed) > 0)
                {
                    return new VerifyResult(lineNumber, FirstUnsortedLine: lineNumber);
                }

                if (line.Length > previous.Length)
                {
                    previous = new byte[line.Length * 2];
                }

                line.CopyTo(previous);
                previousLength = line.Length;
                previousParsed = parsed;
            }
        }

        return new VerifyResult(lineNumber, FirstUnsortedLine: null);
    }
}
