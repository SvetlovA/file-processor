namespace FP.Sorter.Configuration;

public class SorterOptions
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string? TempDirectory { get; set; }
    public long MaxMemoryBytes { get; set; } = 512 * 1024 * 1024; // 512 MB default
    public int MergeWayCount { get; set; } = 16;
    public int BufferSize { get; set; } = 64 * 1024; // 64 KB
    public bool DeleteTempFiles { get; set; } = true;

    public static long ParseSize(string size)
    {
        size = size.Trim().ToUpperInvariant();

        if (size.EndsWith("GB"))
        {
            return (long)(double.Parse(size[..^2]) * 1024 * 1024 * 1024);
        }
        if (size.EndsWith("MB"))
        {
            return (long)(double.Parse(size[..^2]) * 1024 * 1024);
        }
        if (size.EndsWith("KB"))
        {
            return (long)(double.Parse(size[..^2]) * 1024);
        }
        if (size.EndsWith("B"))
        {
            return long.Parse(size[..^1]);
        }

        return long.Parse(size);
    }

    public string GetTempDirectory()
    {
        if (!string.IsNullOrEmpty(TempDirectory))
        {
            return TempDirectory;
        }

        string inputDir = Path.GetDirectoryName(InputPath) ?? Directory.GetCurrentDirectory();
        return Path.Combine(inputDir, $".fp_sort_temp_{Guid.NewGuid():N}");
    }
}
