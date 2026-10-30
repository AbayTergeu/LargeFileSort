using System.Text;
using FileSorter.Features.Sort;
using FileSorter.Features.Sort.Split;

namespace FileSorter.Tests.Features.Sort.Split;

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

    private static List<string> ReadAll(string content, int bufferSize)
    {
        var reader = new ChunkReader(new MemoryStream(Encoding.UTF8.GetBytes(content)), new byte[bufferSize]);
        var chunks = new List<string>();
        for (var chunk = reader.ReadChunk(); !chunk.IsEmpty; chunk = reader.ReadChunk())
        {
            chunks.Add(Encoding.UTF8.GetString(chunk));
        }

        return chunks;
    }
}
