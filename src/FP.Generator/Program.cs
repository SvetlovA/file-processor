using FP.Common.Utilities;
using FP.Generator.CommandLine;
using FP.Generator.Configuration;
using FP.Generator.Generators;
using FP.Generator.Providers;

namespace FP.Generator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = CommandLineBuilder.Build(RunGeneratorAsync);
        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task<int> RunGeneratorAsync(GeneratorOptions options, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Generating file: {options.OutputPath}");
        Console.WriteLine($"Target size: {SizeUtilities.FormatBytes(options.TargetSizeBytes)}");
        Console.WriteLine($"Duplicate percentage: {options.DuplicatePercentage}%");
        if (options.Seed.HasValue)
        {
            Console.WriteLine($"Seed: {options.Seed}");
        }
        Console.WriteLine();

        var contentProvider = new RandomContentProvider(options.Seed);
        var generator = new FileGenerator(contentProvider);

        var progress = new Progress<GenerationProgress>(p =>
        {
            Console.Write($"\rProgress: {p.PercentComplete}% ({SizeUtilities.FormatBytes(p.BytesWritten)} / {SizeUtilities.FormatBytes(p.TotalBytes)})");
        });

        try
        {
            await generator.GenerateAsync(options, progress, cancellationToken);
            Console.WriteLine($"\nFile generated successfully: {options.OutputPath}");
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nGeneration cancelled.");
            return 2;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"\nError: {ex.Message}");
            return 1;
        }
    }

}
