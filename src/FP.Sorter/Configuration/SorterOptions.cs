using FP.Common.Utilities;

namespace FP.Sorter.Configuration;

public class SorterOptions
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string? TempDirectory { get; set; }
    public long MaxMemoryBytes { get; set; } = 512 * 1024 * 1024; // 512 MB default
    public int MergeWayCount { get; set; } = 16;
    public bool DeleteTempFiles { get; set; } = true;

    public static long ParseSize(string size) => SizeUtilities.ParseSize(size);

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
