namespace FP.Sorter.Mergers;

public interface IChunkMerger
{
    Task<string> MergeChunksAsync(
        IReadOnlyList<string> chunkPaths,
        string outputPath,
        string tempDirectory,
        IProgress<MergeProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public record MergeProgress(int MergePass, int TotalPasses, int ChunksRemaining, long LinesProcessed);
