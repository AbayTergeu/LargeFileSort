namespace FileSorter.Features.Sort.Split;

/// <summary>
/// Reads the input in chunks of at most <c>buffer.Length</c> bytes.
/// Every chunk ends at a line break, so no line is split between two chunks;
/// the bytes after the last line break are moved to the start of the next chunk.
/// </summary>
internal sealed class ChunkReader(Stream input, byte[] buffer)
{
    private int _carryStart;
    private int _carryLength;
    private bool _endOfInput;

    /// <summary>
    /// Returns the next chunk, or an empty span at the end of the input.
    /// The span is valid until the next call.
    /// </summary>
    public ReadOnlySpan<byte> ReadChunk()
    {
        buffer.AsSpan(_carryStart, _carryLength).CopyTo(buffer);
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
            return buffer.AsSpan(0, length);
        }

        var chunkLength = buffer.AsSpan(0, length).LastIndexOf((byte)'\n') + 1;
        if (chunkLength == 0)
        {
            throw new LineTooLongException(buffer.Length);
        }

        _carryStart = chunkLength;
        _carryLength = length - chunkLength;
        return buffer.AsSpan(0, chunkLength);
    }
}
