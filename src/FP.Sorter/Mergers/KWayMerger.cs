using System.Text;
using FP.Common.Comparison;
using FP.Common.Interfaces;
using FP.Common.Models;

namespace FP.Sorter.Mergers;

public class KWayMerger(
    int mergeWayCount,
    ILineParser lineParser,
    IComparer<FileLine>? comparer = null,
    bool deleteTempFiles = true)
    : IChunkMerger
{
    private readonly IComparer<FileLine> _comparer = comparer ?? FileLineComparer.Instance;

    public async Task<string> MergeChunksAsync(
        IReadOnlyList<string> chunkPaths,
        string outputPath,
        string tempDirectory,
        IProgress<MergeProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (chunkPaths.Count == 0)
        {
            await File.WriteAllTextAsync(outputPath, string.Empty, cancellationToken);
            return outputPath;
        }

        if (chunkPaths.Count == 1)
        {
            File.Move(chunkPaths[0], outputPath, overwrite: true);
            return outputPath;
        }

        var currentChunks = chunkPaths.ToList();
        var pass = 1;
        var totalPasses = (int)Math.Ceiling(Math.Log(chunkPaths.Count, mergeWayCount));
        long totalLinesProcessed = 0;

        while (currentChunks.Count > 1)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var newChunks = new List<string>();
            var mergeIndex = 0;

            for (var i = 0; i < currentChunks.Count; i += mergeWayCount)
            {
                var chunksToMerge = currentChunks
                    .Skip(i)
                    .Take(mergeWayCount)
                    .ToList();

                var isFinalMerge = currentChunks.Count <= mergeWayCount;
                var mergedPath = isFinalMerge
                    ? outputPath
                    : Path.Combine(tempDirectory, $"merged_pass{pass}_{mergeIndex:D6}.tmp");

                var linesProcessed = await MergeFilesAsync(chunksToMerge, mergedPath, cancellationToken);
                totalLinesProcessed += linesProcessed;

                newChunks.Add(mergedPath);

                if (deleteTempFiles)
                {
                    foreach (var chunk in chunksToMerge)
                    {
                        if (chunk != outputPath)
                        {
                            File.Delete(chunk);
                        }
                    }
                }

                mergeIndex++;
                progress?.Report(new MergeProgress(pass, totalPasses, newChunks.Count + currentChunks.Count - i - chunksToMerge.Count, totalLinesProcessed));
            }

            currentChunks = newChunks;
            pass++;
        }

        if (currentChunks[0] != outputPath)
        {
            File.Move(currentChunks[0], outputPath, overwrite: true);
        }

        return outputPath;
    }

    private async Task<long> MergeFilesAsync(
        IReadOnlyList<string> inputPaths,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var readers = new StreamReader[inputPaths.Count];
        var priorityQueue = new PriorityQueue<(FileLine Line, int ReaderIndex), FileLine>(_comparer);
        long linesProcessed = 0;

        try
        {
            for (var i = 0; i < inputPaths.Count; i++)
            {
                readers[i] = new StreamReader(
                    new FileStream(inputPaths[i], FileMode.Open, FileAccess.Read, FileShare.Read),
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true);

                if (await TryReadNextAsync(readers[i], cancellationToken) is { } fileLine)
                {
                    priorityQueue.Enqueue((fileLine, i), fileLine);
                }
            }

            await using var outputStream = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await using var writer = new StreamWriter(outputStream, Encoding.UTF8);

            while (priorityQueue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (line, readerIndex) = priorityQueue.Dequeue();
                await writer.WriteLineAsync(line.ToLineString());
                linesProcessed++;

                if (await TryReadNextAsync(readers[readerIndex], cancellationToken) is { } nextLine)
                {
                    priorityQueue.Enqueue((nextLine, readerIndex), nextLine);
                }
            }
        }
        finally
        {
            foreach (var reader in readers)
            {
                reader.Dispose();
            }
        }

        return linesProcessed;
    }

    private async Task<FileLine?> TryReadNextAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (lineParser.TryParse(line, out var fileLine))
            {
                return fileLine;
            }
        }
        
        return null;
    }
}
