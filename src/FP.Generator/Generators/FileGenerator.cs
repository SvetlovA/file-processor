using System.Text;
using FP.Generator.Configuration;
using FP.Generator.Providers;

namespace FP.Generator.Generators;

public class FileGenerator(IRandomContentProvider contentProvider) : IFileGenerator
{
    private readonly List<string> _duplicatePool = new();
    private const int MaxDuplicatePoolSize = 1000;

    public async Task GenerateAsync(
        GeneratorOptions options,
        IProgress<GenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long bytesWritten = 0;
        var lastReportedPercent = -1;

        await using var stream = new FileStream(
            options.OutputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await using var writer = new StreamWriter(stream, Encoding.UTF8);

        while (bytesWritten < options.TargetSizeBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = GenerateLine(options);
            await writer.WriteLineAsync(line);

            bytesWritten += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;

            var percentComplete = (int)(bytesWritten * 100 / options.TargetSizeBytes);
            if (percentComplete != lastReportedPercent)
            {
                lastReportedPercent = percentComplete;
                progress?.Report(new GenerationProgress(bytesWritten, options.TargetSizeBytes, Math.Min(percentComplete, 100)));
            }
        }

        progress?.Report(new GenerationProgress(bytesWritten, options.TargetSizeBytes, 100));
    }

    private string GenerateLine(GeneratorOptions options)
    {
        string text;

        if (_duplicatePool.Count > 0 && contentProvider.ShouldUseDuplicate(options.DuplicatePercentage))
        {
            text = _duplicatePool[(int)contentProvider.GenerateNumber(0, _duplicatePool.Count - 1)];
        }
        else
        {
            text = contentProvider.GenerateString(options.MinStringLength, options.MaxStringLength);

            if (_duplicatePool.Count < MaxDuplicatePoolSize)
            {
                _duplicatePool.Add(text);
            }
        }

        var number = contentProvider.GenerateNumber(options.MinNumber, options.MaxNumber);
        return $"{number}. {text}";
    }
}
