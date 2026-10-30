using System.Collections.Concurrent;

namespace FileSorter.Features.Sort.Split;

/// <summary>
/// A fixed number of chunk workspaces shared between the reader and the sort workers.
/// When all of them are in use, <see cref="Rent"/> waits until one is returned:
/// this limits the memory and stops the reader from running ahead of the workers.
/// Workspaces are created on first use, so a small input does not allocate all of them.
/// </summary>
internal sealed class ChunkPool(int count, int chunkSize) : IDisposable
{
    private readonly BlockingCollection<ChunkWorkspace> _free = new(count);
    private int _created;

    /// <summary>
    /// Must be called from one thread only (the reader).
    /// </summary>
    public ChunkWorkspace Rent()
    {
        if (_free.TryTake(out var workspace))
        {
            return workspace;
        }

        if (_created < count)
        {
            _created++;
            return new ChunkWorkspace(new byte[chunkSize]);
        }

        return _free.Take();
    }

    public void Return(ChunkWorkspace workspace) => _free.Add(workspace);

    public void Dispose() => _free.Dispose();
}
