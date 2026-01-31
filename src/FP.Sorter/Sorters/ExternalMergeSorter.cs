using FP.Sorter.Configuration;
using FP.Sorter.Mergers;

namespace FP.Sorter.Sorters;

public class ExternalMergeSorter(
    SorterOptions options,
    IChunkSorter chunkSorter,
    IChunkMerger chunkMerger)
    : IExternalSorter
{
    public async Task SortAsync(
        string inputPath,
        string outputPath,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInputFile(inputPath);

        var tempDirectory = options.GetTempDirectory();
        var tempCreated = false;

        try
        {
            progress?.Report(new SortProgress(SortPhase.Analyzing, 0, "Analyzing input file..."));

            var fileInfo = new FileInfo(inputPath);
            var totalBytes = fileInfo.Length;

            progress?.Report(new SortProgress(SortPhase.CreatingChunks, 0, $"Creating sorted chunks from {FormatBytes(totalBytes)} input..."));

            var chunkProgress = new Progress<ChunkProgress>(p =>
            {
                var percent = (int)(p.BytesProcessed * 100 / p.TotalBytes);
                progress?.Report(new SortProgress(
                    SortPhase.CreatingChunks,
                    percent,
                    $"Created {p.ChunksCreated} chunks, processed {FormatBytes(p.BytesProcessed)} / {FormatBytes(p.TotalBytes)}"));
            });

            var chunkPaths = await chunkSorter.CreateSortedChunksAsync(
                inputPath,
                tempDirectory,
                chunkProgress,
                cancellationToken);

            tempCreated = true;

            if (chunkPaths.Count == 0)
            {
                await File.WriteAllTextAsync(outputPath, string.Empty, cancellationToken);
                progress?.Report(new SortProgress(SortPhase.Completed, 100, "Completed (empty file)"));
                return;
            }

            progress?.Report(new SortProgress(SortPhase.Merging, 0, $"Merging {chunkPaths.Count} chunks..."));

            var mergeProgress = new Progress<MergeProgress>(p =>
            {
                var estimatedPercent = Math.Min(99, (p.MergePass * 100) / Math.Max(1, p.TotalPasses));
                progress?.Report(new SortProgress(
                    SortPhase.Merging,
                    estimatedPercent,
                    $"Merge pass {p.MergePass}/{p.TotalPasses}, {p.ChunksRemaining} chunks remaining, {p.LinesProcessed:N0} lines processed"));
            });

            await chunkMerger.MergeChunksAsync(
                chunkPaths,
                outputPath,
                tempDirectory,
                mergeProgress,
                cancellationToken);

            progress?.Report(new SortProgress(SortPhase.Finalizing, 99, "Finalizing..."));
            progress?.Report(new SortProgress(SortPhase.Completed, 100, $"Sorting completed. Output: {outputPath}"));
        }
        finally
        {
            if (tempCreated && options.DeleteTempFiles && Directory.Exists(tempDirectory))
            {
                try
                {
                    Directory.Delete(tempDirectory, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
    }

    private static void ValidateInputFile(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}", inputPath);
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var index = 0;
        double size = bytes;

        while (size >= 1024 && index < suffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return $"{size:F2} {suffixes[index]}";
    }
}
