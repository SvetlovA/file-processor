using FP.Common.Utilities;

namespace FP.Generator.Configuration;

public class GeneratorOptions
{
    public long TargetSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB default
    public string OutputPath { get; set; } = "output.txt";
    public int DuplicatePercentage { get; set; } = 10;
    public int MinStringLength { get; set; } = 5;
    public int MaxStringLength { get; set; } = 50;
    public long MinNumber { get; set; } = 1;
    public long MaxNumber { get; set; } = 100_000_000;
    public int? Seed { get; set; }

    public static long ParseSize(string size) => SizeUtilities.ParseSize(size);
}
