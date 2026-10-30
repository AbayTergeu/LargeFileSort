using FileSorter.Lines;

namespace FileSorter.Features.Sort.Split;

/// <summary>
/// The memory for one chunk: its bytes and the index of its lines.
/// Both are reused for the next chunk, so sorting does not allocate per chunk.
/// </summary>
internal sealed class ChunkWorkspace(byte[] buffer)
{
    public byte[] Buffer { get; } = buffer;

    public List<LineRef> Lines { get; } = [];
}

/// <summary>
/// A line inside <see cref="ChunkWorkspace.Buffer"/>: where it is and its parsed parts.
/// </summary>
internal readonly record struct LineRef(int Offset, int Length, ParsedLine Parsed);
