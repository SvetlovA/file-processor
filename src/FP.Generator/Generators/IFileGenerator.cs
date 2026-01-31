using FP.Generator.Configuration;

namespace FP.Generator.Generators;

public interface IFileGenerator
{
    Task GenerateAsync(GeneratorOptions options, IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default);
}

public record GenerationProgress(long BytesWritten, long TotalBytes, int PercentComplete);
