namespace FP.Sorter.Sorters;

public interface IChunkSorter
{
    Task<IReadOnlyList<string>> CreateSortedChunksAsync(
        string inputPath,
        string tempDirectory,
        IProgress<ChunkProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public record ChunkProgress(int ChunksCreated, long BytesProcessed, long TotalBytes);
