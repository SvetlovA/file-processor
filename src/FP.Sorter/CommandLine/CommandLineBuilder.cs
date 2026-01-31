using System.CommandLine;
using FP.Sorter.Configuration;

namespace FP.Sorter.CommandLine;

public static class CommandLineBuilder
{
    public static RootCommand Build(Func<SorterOptions, CancellationToken, Task<int>> handler)
    {
        var inputOption = new Option<FileInfo>("-i", ["--input"])
        {
            Description = "Input file to sort (required)",
            Required = true
        };

        var outputOption = new Option<string?>("-o", ["--output"])
        {
            Description = "Output file path. Default: <input>_sorted.<ext>"
        };

        var memoryOption = new Option<string>("-m", ["--memory"])
        {
            Description = "Max memory to use (e.g., 512MB, 1GB)",
            DefaultValueFactory = _ => "512MB"
        };

        var mergeWayOption = new Option<int>("-k", ["--merge-way"])
        {
            Description = "Number of chunks to merge at once",
            DefaultValueFactory = _ => 16
        };

        var tempOption = new Option<string?>("-t", ["--temp"])
        {
            Description = "Temporary directory for chunks"
        };

        var keepTempOption = new Option<bool>("--keep-temp")
        {
            Description = "Don't delete temporary files after sorting",
            DefaultValueFactory = _ => false
        };

        var rootCommand = new RootCommand("FP.Sorter - External Merge Sort for Large Files")
        {
            inputOption,
            outputOption,
            memoryOption,
            mergeWayOption,
            tempOption,
            keepTempOption
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var input = parseResult.GetValue(inputOption)!;
            var output = parseResult.GetValue(outputOption);
            var memory = parseResult.GetValue(memoryOption)!;
            var mergeWay = parseResult.GetValue(mergeWayOption);
            var temp = parseResult.GetValue(tempOption);
            var keepTemp = parseResult.GetValue(keepTempOption);

            var outputPath = output;
            if (string.IsNullOrEmpty(outputPath))
            {
                var inputDir = Path.GetDirectoryName(input.FullName) ?? Directory.GetCurrentDirectory();
                var inputName = Path.GetFileNameWithoutExtension(input.FullName);
                var inputExt = Path.GetExtension(input.FullName);
                outputPath = Path.Combine(inputDir, $"{inputName}_sorted{inputExt}");
            }

            var options = new SorterOptions
            {
                InputPath = input.FullName,
                OutputPath = outputPath,
                MaxMemoryBytes = SorterOptions.ParseSize(memory),
                MergeWayCount = mergeWay,
                TempDirectory = temp,
                DeleteTempFiles = !keepTemp
            };

            return await handler(options, cancellationToken);
        });

        return rootCommand;
    }
}
