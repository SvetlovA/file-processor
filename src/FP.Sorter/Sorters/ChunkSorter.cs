using System.Text;
using FP.Common.Comparison;
using FP.Common.Interfaces;
using FP.Common.Models;

namespace FP.Sorter.Sorters;

public class ChunkSorter(
    ILineParser lineParser,
    IComparer<FileLine>? comparer = null,
    long chunkSizeBytes = 512 * 1024 * 1024,
    int bufferSize = 64 * 1024)
    : IChunkSorter
{
    private readonly IComparer<FileLine> _comparer = comparer ?? FileLineComparer.Instance;

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
            FileShare.Read,
            bufferSize,
            FileOptions.SequentialScan | FileOptions.Asynchronous);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize);

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
                currentChunk.Add(fileLine);
                currentChunkSize += lineSize + EstimateObjectOverhead(fileLine);
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
            FileShare.None,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var writer = new StreamWriter(stream, Encoding.UTF8, bufferSize);

        foreach (var line in chunk)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(line.OriginalLine);
        }

        return chunkPath;
    }

    private static long EstimateObjectOverhead(FileLine fileLine)
    {
        return 32 + fileLine.Text.Length * 2 + fileLine.OriginalLine.Length * 2;
    }
}
