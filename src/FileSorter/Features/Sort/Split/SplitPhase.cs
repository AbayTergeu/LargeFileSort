using System.Runtime.InteropServices;
using FileSorter.Lines;

namespace FileSorter.Features.Sort.Split;

/// <summary>
/// Phase 1: cuts the input into chunks that fit in memory, sorts each chunk
/// and writes it to a temporary "run" file.
/// </summary>
internal sealed class SplitPhase(SortOptions options, TempDirectory tempDirectory)
{
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    private readonly List<LineRef> _lines = [];
    private long _lineNumber;

    public long LineCount { get; private set; }

    public long SkippedLineCount { get; private set; }

    public List<string> Run(Stream input)
    {
        var buffer = new byte[options.ChunkSize];
        var reader = new ChunkReader(input, buffer);
        var runs = new List<string>();

        var isFirstChunk = true;
        for (var chunk = reader.ReadChunk(); !chunk.IsEmpty; chunk = reader.ReadChunk())
        {
            var start = isFirstChunk && chunk.StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
            isFirstChunk = false;

            ParseLines(buffer, start, chunk.Length);
            if (_lines.Count == 0)
            {
                continue;
            }

            SortLines(buffer);
            runs.Add(WriteRun(buffer));
        }

        return runs;
    }

    private void ParseLines(byte[] buffer, int start, int end)
    {
        _lines.Clear();
        while (start < end)
        {
            var remaining = buffer.AsSpan(start, end - start);
            var lineBreak = remaining.IndexOf((byte)'\n');
            var line = lineBreak < 0 ? remaining : remaining[..lineBreak];
            if (line.EndsWith((byte)'\r'))
            {
                line = line[..^1];
            }

            _lineNumber++;
            if (LineParser.TryParse(line, out var parsed))
            {
                _lines.Add(new LineRef(start, line.Length, parsed));
                LineCount++;
            }
            else if (options.SkipInvalid)
            {
                SkippedLineCount++;
            }
            else
            {
                throw new InvalidLineException(_lineNumber, line);
            }

            start += lineBreak < 0 ? remaining.Length : lineBreak + 1;
        }
    }

    private void SortLines(byte[] buffer)
    {
        CollectionsMarshal.AsSpan(_lines).Sort((x, y) => LineComparer.Compare(
            buffer.AsSpan(x.Offset, x.Length), x.Parsed,
            buffer.AsSpan(y.Offset, y.Length), y.Parsed));
    }

    private string WriteRun(byte[] buffer)
    {
        var path = tempDirectory.NewFilePath();
        using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1 << 20);
        foreach (var line in _lines)
        {
            output.Write(buffer, line.Offset, line.Length);
            output.WriteByte((byte)'\n');
        }

        return path;
    }

    private readonly record struct LineRef(int Offset, int Length, ParsedLine Parsed);
}
