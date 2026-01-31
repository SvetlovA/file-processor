using FP.Common.Parsing;
using FP.Generator.Configuration;
using FP.Generator.Generators;
using FP.Generator.Providers;
using Moq;
using NUnit.Framework;

namespace FP.Generator.Tests.Services;

[TestFixture]
public class TestsFileGenerator
{
    private string _testDirectory = null!;
    private string _testFilePath = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FP.Generator.Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "test_output.txt");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [Test]
    public async Task TestsGenerateAsync_CreatesFileWithCorrectFormat()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 1024,
            OutputPath = _testFilePath,
            DuplicatePercentage = 0
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);
        var parser = new LineParser();

        await generator.GenerateAsync(options);

        var lines = await File.ReadAllLinesAsync(_testFilePath);
        Assert.That(lines.Length, Is.GreaterThan(0));

        foreach (var line in lines)
        {
            Assert.That(parser.TryParse(line, out _), Is.True, $"Invalid line format: {line}");
        }
    }

    [Test]
    public async Task TestsGenerateAsync_GeneratesApproximateTargetSize()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 10 * 1024,
            OutputPath = _testFilePath,
            DuplicatePercentage = 0
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);

        await generator.GenerateAsync(options);

        var fileInfo = new FileInfo(_testFilePath);
        Assert.That(fileInfo.Length, Is.GreaterThanOrEqualTo(options.TargetSizeBytes));
        Assert.That(fileInfo.Length, Is.LessThan(options.TargetSizeBytes * 1.1));
    }

    [Test]
    public async Task TestsGenerateAsync_ReportsProgress()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 5 * 1024,
            OutputPath = _testFilePath
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);
        var progressReports = new System.Collections.Concurrent.ConcurrentBag<GenerationProgress>();

        var progress = new Progress<GenerationProgress>(p => progressReports.Add(p));

        await generator.GenerateAsync(options, progress);
        await Task.Delay(500);

        Assert.That(progressReports.Count, Is.GreaterThan(0));
        Assert.That(progressReports.Any(p => p.PercentComplete == 100), Is.True);
    }

    [Test]
    public async Task TestsGenerateAsync_WithDuplicates_GeneratesDuplicateStrings()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 50 * 1024,
            OutputPath = _testFilePath,
            DuplicatePercentage = 50
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);
        var parser = new LineParser();

        await generator.GenerateAsync(options);

        var lines = await File.ReadAllLinesAsync(_testFilePath);
        var texts = lines
            .Select(l => parser.TryParse(l, out var fl) ? fl.Text : null)
            .Where(t => t != null)
            .ToList();

        var uniqueTexts = texts.Distinct().Count();
        var totalTexts = texts.Count;

        Assert.That(uniqueTexts, Is.LessThan(totalTexts));
    }

    [Test]
    public async Task TestsGenerateAsync_WithSeed_ProducesReproducibleOutput()
    {
        var options1 = new GeneratorOptions
        {
            TargetSizeBytes = 1024,
            OutputPath = _testFilePath,
            Seed = 42
        };
        var provider1 = new RandomContentProvider(options1.Seed);
        var generator1 = new FileGenerator(provider1);

        await generator1.GenerateAsync(options1);
        var content1 = await File.ReadAllTextAsync(_testFilePath);

        var secondFilePath = Path.Combine(_testDirectory, "test_output2.txt");
        var options2 = new GeneratorOptions
        {
            TargetSizeBytes = 1024,
            OutputPath = secondFilePath,
            Seed = 42
        };
        var provider2 = new RandomContentProvider(options2.Seed);
        var generator2 = new FileGenerator(provider2);

        await generator2.GenerateAsync(options2);
        var content2 = await File.ReadAllTextAsync(secondFilePath);

        Assert.That(content1, Is.EqualTo(content2));
    }

    [Test]
    public async Task TestsGenerateAsync_WithCancellation_StopsGeneration()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 100 * 1024 * 1024,
            OutputPath = _testFilePath
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await generator.GenerateAsync(options, cancellationToken: cts.Token));
    }

    [Test]
    public async Task TestsGenerateAsync_NumbersWithinConfiguredRange()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 5 * 1024,
            OutputPath = _testFilePath,
            MinNumber = 100,
            MaxNumber = 200
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);
        var parser = new LineParser();

        await generator.GenerateAsync(options);

        var lines = await File.ReadAllLinesAsync(_testFilePath);
        foreach (var line in lines)
        {
            Assert.That(parser.TryParse(line, out var fileLine), Is.True);
            Assert.That(fileLine.Number, Is.InRange(options.MinNumber, options.MaxNumber));
        }
    }

    [Test]
    public async Task TestsGenerateAsync_StringsWithinConfiguredLength()
    {
        var options = new GeneratorOptions
        {
            TargetSizeBytes = 5 * 1024,
            OutputPath = _testFilePath,
            MinStringLength = 10,
            MaxStringLength = 20,
            DuplicatePercentage = 0
        };
        var contentProvider = new RandomContentProvider(seed: 42);
        var generator = new FileGenerator(contentProvider);
        var parser = new LineParser();

        await generator.GenerateAsync(options);

        var lines = await File.ReadAllLinesAsync(_testFilePath);
        foreach (var line in lines)
        {
            Assert.That(parser.TryParse(line, out var fileLine), Is.True);
            Assert.That(fileLine.Text.Length, Is.InRange(options.MinStringLength, options.MaxStringLength));
        }
    }
}
