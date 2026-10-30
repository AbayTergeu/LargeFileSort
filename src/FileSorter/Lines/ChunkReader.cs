using FileSorter.Features.Sort;

namespace FileSorter.Lines;

/// <summary>
/// Reads the input in chunks. Every chunk ends at a line break, so no line is split between
/// two chunks; the bytes after the last line break are moved to the start of the next chunk.
/// Each call can use a different buffer: the carried bytes are copied from the previous one.
/// </summary>
internal sealed class ChunkReader(Stream input)
{
    private byte[] _carrySource = [];
    private int _carryStart;
    private int _carryLength;
    private bool _endOfInput;

    /// <summary>
    /// Fills <paramref name="buffer"/> with the next chunk and returns its length,
    /// or 0 at the end of the input.
    /// </summary>
    public int ReadChunk(byte[] buffer)
    {
        _carrySource.AsSpan(_carryStart, _carryLength).CopyTo(buffer);
        var length = _carryLength;

        while (length < buffer.Length && !_endOfInput)
        {
            var read = input.Read(buffer, length, buffer.Length - length);
            _endOfInput = read == 0;
            length += read;
        }

        if (_endOfInput)
        {
            _carryLength = 0;
            return length;
        }

        var chunkLength = buffer.AsSpan(0, length).LastIndexOf((byte)'\n') + 1;
        if (chunkLength == 0)
        {
            throw new LineTooLongException(buffer.Length);
        }

        _carrySource = buffer;
        _carryStart = chunkLength;
        _carryLength = length - chunkLength;
        return chunkLength;
    }
}
