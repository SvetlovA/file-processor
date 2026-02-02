using System.Text;
using FP.Common.Comparison;
using FP.Common.Interfaces;
using FP.Common.Models;

namespace FP.Sorter.Sorters;

public class ChunkSorter(
    ILineParser lineParser,
    IComparer<FileLine>? comparer = null,
    long chunkSizeBytes = 512 * 1024 * 1024)
    : IChunkSorter
{
    private readonly IComparer<FileLine> _comparer = comparer ?? FileLineComparer.Instance;
    private readonly HashSet<string> _stringPool = new();

    public async Task<IReadOnlyList<string>> CreateSortedChunksAsync(
        string inputPath,
        string tempDirectory,
        IProgress<ChunkProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(tempDirectory);

        var chunkPaths = new List<string>();
        var fileInfo = new FileInfo(inputPath);
        var totalBytes = fileInfo.Length;
        long bytesProcessed = 0;

        await using var stream = new FileStream(
            inputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var currentChunk = new List<FileLine>();
        long currentChunkSize = 0;
        var chunkIndex = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long lineSize = Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
            bytesProcessed += lineSize;

            if (lineParser.TryParse(line, out var fileLine))
            {
                var pooledLine = new FileLine(fileLine.Number, PoolString(fileLine.Text));
                currentChunk.Add(pooledLine);
                currentChunkSize += pooledLine.GetUtf8ByteCount() + ObjectOverhead;
            }

            if (currentChunkSize >= chunkSizeBytes)
            {
                var chunkPath = await WriteSortedChunkAsync(
                    currentChunk,
                    tempDirectory,
                    chunkIndex++,
                    cancellationToken);

                chunkPaths.Add(chunkPath);
                currentChunk.Clear();
                currentChunkSize = 0;
                _stringPool.Clear();

                progress?.Report(new ChunkProgress(chunkPaths.Count, bytesProcessed, totalBytes));
            }
        }

        if (currentChunk.Count > 0)
        {
            var chunkPath = await WriteSortedChunkAsync(
                currentChunk,
                tempDirectory,
                chunkIndex,
                cancellationToken);

            chunkPaths.Add(chunkPath);
            _stringPool.Clear();
            progress?.Report(new ChunkProgress(chunkPaths.Count, bytesProcessed, totalBytes));
        }

        return chunkPaths;
    }

    private async Task<string> WriteSortedChunkAsync(
        List<FileLine> chunk,
        string tempDirectory,
        int chunkIndex,
        CancellationToken cancellationToken)
    {
        chunk.Sort(_comparer);

        var chunkPath = Path.Combine(tempDirectory, $"chunk_{chunkIndex:D6}.tmp");

        await using var stream = new FileStream(
            chunkPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        foreach (var line in chunk)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(line.ToLineString());
        }

        return chunkPath;
    }

    private string PoolString(string value)
    {
        if (_stringPool.TryGetValue(value, out var pooled))
            return pooled;
        _stringPool.Add(value);
        return value;
    }

    // Fixed overhead per FileLine struct: 8 bytes (long) + 8 bytes (string ref) + padding
    private const int ObjectOverhead = 24;
}
