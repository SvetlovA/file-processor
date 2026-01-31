using System.CommandLine;
using FP.Generator.Configuration;

namespace FP.Generator.CommandLine;

public static class CommandLineBuilder
{
    public static RootCommand Build(Func<GeneratorOptions, CancellationToken, Task<int>> handler)
    {
        var sizeOption = new Option<string>("-s", ["--size"])
        {
            Description = "Target file size (e.g., 10MB, 1GB)",
            DefaultValueFactory = _ => "10MB"
        };

        var outputOption = new Option<string>("-o", ["--output"])
        {
            Description = "Output file path",
            DefaultValueFactory = _ => "output.txt"
        };

        var duplicatesOption = new Option<int>("-d", ["--duplicates"])
        {
            Description = "Duplicate percentage (0-100)",
            DefaultValueFactory = _ => 10
        };

        var seedOption = new Option<int?>("--seed")
        {
            Description = "Random seed for reproducible generation"
        };

        var minStringLengthOption = new Option<int>("--min-string-length")
        {
            Description = "Minimum string length",
            DefaultValueFactory = _ => 5
        };

        var maxStringLengthOption = new Option<int>("--max-string-length")
        {
            Description = "Maximum string length",
            DefaultValueFactory = _ => 50
        };

        var rootCommand = new RootCommand("FP.Generator - Test File Generator for creating files with format '<Number>. <String>'")
        {
            sizeOption,
            outputOption,
            duplicatesOption,
            seedOption,
            minStringLengthOption,
            maxStringLengthOption
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var size = parseResult.GetValue(sizeOption)!;
            var output = parseResult.GetValue(outputOption)!;
            var duplicates = parseResult.GetValue(duplicatesOption);
            var seed = parseResult.GetValue(seedOption);
            var minStringLength = parseResult.GetValue(minStringLengthOption);
            var maxStringLength = parseResult.GetValue(maxStringLengthOption);

            var options = new GeneratorOptions
            {
                TargetSizeBytes = GeneratorOptions.ParseSize(size),
                OutputPath = output,
                DuplicatePercentage = duplicates,
                Seed = seed,
                MinStringLength = minStringLength,
                MaxStringLength = maxStringLength
            };

            return await handler(options, cancellationToken);
        });

        return rootCommand;
    }
}
