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
    public int BufferSize { get; set; } = 64 * 1024; // 64 KB

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
}
