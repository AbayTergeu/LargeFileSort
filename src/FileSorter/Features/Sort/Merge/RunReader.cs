using FileSorter.Lines;

namespace FileSorter.Features.Sort.Merge;

/// <summary>
/// Reads a sorted run line by line through its own buffer.
/// Runs are written by the sorter itself, so every line is valid and ends with '\n'.
/// </summary>
internal sealed class RunReader(string path, int bufferSize) : IDisposable
{
    private readonly FileStream _input = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0, FileOptions.SequentialScan);
    private byte[] _buffer = new byte[bufferSize];
    private int _position;
    private int _dataEnd;
    private int _lineStart;
    private int _lineLength;

    /// <summary>
    /// The current line without '\n'. Valid until the next <see cref="MoveNext"/>.
    /// </summary>
    public ReadOnlySpan<byte> Current => _buffer.AsSpan(_lineStart, _lineLength);

    public ParsedLine CurrentParsed { get; private set; }

    /// <summary>
    /// Moves to the next line. Returns false at the end of the run.
    /// </summary>
    public bool MoveNext()
    {
        while (true)
        {
            var lineBreak = _buffer.AsSpan(_position, _dataEnd - _position).IndexOf((byte)'\n');
            if (lineBreak >= 0)
            {
                _lineStart = _position;
                _lineLength = lineBreak;
                _position += lineBreak + 1;

                if (!LineParser.TryParse(Current, out var parsed))
                {
                    throw new InvalidDataException($"Run file '{_input.Name}' is corrupted.");
                }

                CurrentParsed = parsed;
                return true;
            }

            if (!ReadMore())
            {
                return false;
            }
        }
    }

    public void Dispose() => _input.Dispose();

    private bool ReadMore()
    {
        if (_position > 0)
        {
            // The rest of the buffer holds the start of the next line: move it to the front.
            _buffer.AsSpan(_position, _dataEnd - _position).CopyTo(_buffer);
            _dataEnd -= _position;
            _position = 0;
        }
        else if (_dataEnd == _buffer.Length)
        {
            // A single line is longer than the buffer.
            Array.Resize(ref _buffer, _buffer.Length * 2);
        }

        var read = _input.Read(_buffer, _dataEnd, _buffer.Length - _dataEnd);
        if (read == 0 && _dataEnd > 0)
        {
            throw new InvalidDataException($"Run file '{_input.Name}' does not end with a line break.");
        }

        _dataEnd += read;
        return read > 0;
    }
}
