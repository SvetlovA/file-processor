namespace FP.Sorter.Sorters;

public interface IExternalSorter
{
    Task SortAsync(
        string inputPath,
        string outputPath,
        IProgress<SortProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public record SortProgress(SortPhase Phase, int PercentComplete, string Message);

public enum SortPhase
{
    Analyzing,
    CreatingChunks,
    Merging,
    Finalizing,
    Completed
}
