namespace FileSorter.Features.Sort;

/// <summary>
/// A folder for intermediate run files. It is deleted with everything in it on dispose,
/// so no temporary files are left behind, even when sorting fails.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    private int _fileCount;
    
    public string NewFilePath() => System.IO.Path.Combine(Path, $"run-{++_fileCount:D6}.tmp");

    public TempDirectory(string parent)
    {
        Path = System.IO.Path.Combine(parent, $".sort-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }
    
    private string Path { get; }
    
    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
