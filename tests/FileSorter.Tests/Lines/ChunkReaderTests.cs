using System.Text;
using FileSorter.Lines;

namespace FileSorter.Tests.Lines;

public class ChunkReaderTests
{
    [Fact]
    public void ReadChunk_CutsAtLastLineBreakAndCarriesTheRest()
    {
        var chunks = ReadAll("1. A\n2. BB\n3. CCC\n", bufferSize: 8);

        Assert.Equal(["1. A\n", "2. BB\n", "3. CCC\n"], chunks);
    }

    [Fact]
    public void ReadChunk_LineEndingExactlyAtBufferEnd()
    {
        var chunks = ReadAll("1. AB\n2. CD\n", bufferSize: 6);

        Assert.Equal(["1. AB\n", "2. CD\n"], chunks);
    }

    [Fact]
    public void ReadChunk_LastLineWithoutLineBreak()
    {
        var chunks = ReadAll("1. A\n2. B", bufferSize: 6);

        Assert.Equal(["1. A\n", "2. B"], chunks);
    }
    
    [Fact]
    public void ReadChunk_SameBufferEveryTime()
    {
        var chunks = ReadAll("1. A\n2. BB\n3. CCC\n", bufferSize: 8, bufferCount: 1);

        Assert.Equal(["1. A\n", "2. BB\n", "3. CCC\n"], chunks);
    }

    [Fact]
    public void ReadChunk_EmptyInput_ReturnsNoChunks()
    {
        Assert.Empty(ReadAll("", bufferSize: 8));
    }

    [Fact]
    public void ReadChunk_LineLongerThanBuffer_Throws()
    {
        Assert.Throws<LineTooLongException>(() => ReadAll("1. Too long\n", bufferSize: 8));
    }

    [Fact]
    public void ReadChunk_LastLineLongerThanBufferWithoutLineBreak_Throws()
    {
        Assert.Throws<LineTooLongException>(() => ReadAll("1. A\n2. Too long", bufferSize: 8));
    }

    /// <summary>
    /// Reads all chunks, using the buffers in turn, as the sorter does.
    /// </summary>
    private static List<string> ReadAll(string content, int bufferSize, int bufferCount = 2)
    {
        var reader = new ChunkReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
        var buffers = Enumerable.Range(0, bufferCount).Select(_ => new byte[bufferSize]).ToArray();
        var chunks = new List<string>();

        for (var i = 0; ; i++)
        {
            var buffer = buffers[i % bufferCount];
            var length = reader.ReadChunk(buffer);
            if (length == 0)
            {
                return chunks;
            }

            chunks.Add(Encoding.UTF8.GetString(buffer, 0, length));
        }
    }
}
