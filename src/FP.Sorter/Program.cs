using FP.Common.Comparison;
using FP.Common.Parsing;
using FP.Sorter.CommandLine;
using FP.Sorter.Configuration;
using FP.Sorter.Mergers;
using FP.Sorter.Sorters;

namespace FP.Sorter;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = CommandLineBuilder.Build(RunSorterAsync);
        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task<int> RunSorterAsync(SorterOptions options, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Input file: {options.InputPath}");
        Console.WriteLine($"Output file: {options.OutputPath}");
        Console.WriteLine($"Max memory: {FormatBytes(options.MaxMemoryBytes)}");
        Console.WriteLine($"Merge way count: {options.MergeWayCount}");
        Console.WriteLine();

        var lineParser = new LineParser();
        var comparer = FileLineComparer.Instance;
        var chunkSorter = new ChunkSorter(lineParser, comparer, options.MaxMemoryBytes, options.BufferSize);
        var chunkMerger = new KWayMerger(options.MergeWayCount, lineParser, comparer, options.BufferSize, options.DeleteTempFiles);
        var sorter = new ExternalMergeSorter(options, chunkSorter, chunkMerger);

        var progress = new Progress<SortProgress>(p =>
        {
            var width = 80;
            try { width = Console.WindowWidth; } catch { }
            Console.Write($"\r[{p.Phase}] {p.PercentComplete}% - {p.Message}".PadRight(width - 1));
        });

        try
        {
            await sorter.SortAsync(options.InputPath, options.OutputPath, progress, cancellationToken);
            Console.WriteLine();
            Console.WriteLine($"File sorted successfully: {options.OutputPath}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nSorting cancelled.");
            return 2;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"\nError: {ex.Message}");
            return 1;
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
