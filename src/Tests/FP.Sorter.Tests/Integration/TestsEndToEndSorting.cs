using FP.Common.Parsing;
using FP.Sorter.Configuration;
using FP.Sorter.Mergers;
using FP.Sorter.Sorters;
using NUnit.Framework;

namespace FP.Sorter.Tests.Integration;

[TestFixture]
public class TestsEndToEndSorting
{
    private string _testDirectory = null!;
    private string _inputFilePath = null!;
    private string _outputFilePath = null!;
    private LineParser _lineParser = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FP.Sorter.Integration_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _inputFilePath = Path.Combine(_testDirectory, "input.txt");
        _outputFilePath = Path.Combine(_testDirectory, "output.txt");
        _lineParser = new LineParser();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    private ExternalMergeSorter CreateSorter(long maxMemory = 1024 * 1024, int mergeWayCount = 4)
    {
        var options = new SorterOptions
        {
            InputPath = _inputFilePath,
            OutputPath = _outputFilePath,
            MaxMemoryBytes = maxMemory,
            MergeWayCount = mergeWayCount,
            DeleteTempFiles = true
        };
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: options.MaxMemoryBytes);
        var chunkMerger = new KWayMerger(options.MergeWayCount, _lineParser);

        return new ExternalMergeSorter(options, chunkSorter, chunkMerger);
    }

    [Test]
    public async Task TestsSort_SimpleFile_SortsCorrectly()
    {
        var inputLines = new[]
        {
            "3. Cherry",
            "1. Apple",
            "2. Banana"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        var expected = new[] { "1. Apple", "2. Banana", "3. Cherry" };
        Assert.That(outputLines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsSort_SameTextDifferentNumbers_SortsByNumber()
    {
        var inputLines = new[]
        {
            "5. Apple",
            "1. Apple",
            "3. Apple",
            "2. Apple",
            "4. Apple"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        var expected = new[] { "1. Apple", "2. Apple", "3. Apple", "4. Apple", "5. Apple" };
        Assert.That(outputLines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsSort_MixedContent_SortsTextFirstThenNumber()
    {
        var inputLines = new[]
        {
            "2. Banana",
            "1. Apple",
            "3. Apple",
            "2. Apple",
            "1. Banana"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        var expected = new[] { "1. Apple", "2. Apple", "3. Apple", "1. Banana", "2. Banana" };
        Assert.That(outputLines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsSort_EmptyFile_CreatesEmptyOutput()
    {
        await File.WriteAllTextAsync(_inputFilePath, string.Empty);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        Assert.That(File.Exists(_outputFilePath), Is.True);
        Assert.That(await File.ReadAllTextAsync(_outputFilePath), Is.Empty);
    }

    [Test]
    public async Task TestsSort_SingleLine_OutputsSameLine()
    {
        await File.WriteAllLinesAsync(_inputFilePath, new[] { "1. Only" });

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines, Is.EqualTo(new[] { "1. Only" }));
    }

    [Test]
    public async Task TestsSort_LargeFile_WithMultipleChunks()
    {
        var random = new Random(42);
        var inputLines = Enumerable.Range(1, 1000)
            .Select(_ => $"{random.Next(1, 10000)}. Text{(char)('A' + random.Next(26))}{random.Next(1000)}")
            .ToList();

        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter(maxMemory: 10 * 1024, mergeWayCount: 4);
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines.Length, Is.EqualTo(1000));

        for (int i = 1; i < outputLines.Length; i++)
        {
            _lineParser.TryParse(outputLines[i - 1], out var prev);
            _lineParser.TryParse(outputLines[i], out var curr);

            int textComparison = string.Compare(prev.Text, curr.Text, StringComparison.Ordinal);
            if (textComparison == 0)
            {
                Assert.That(prev.Number, Is.LessThanOrEqualTo(curr.Number),
                    $"Lines not sorted correctly at index {i}: {prev.ToLineString()} vs {curr.ToLineString()}");
            }
            else
            {
                Assert.That(textComparison, Is.LessThan(0),
                    $"Lines not sorted correctly at index {i}: {prev.ToLineString()} vs {curr.ToLineString()}");
            }
        }
    }

    [Test]
    public async Task TestsSort_WithDuplicateLines_PreservesAll()
    {
        var inputLines = new[]
        {
            "1. Apple",
            "1. Apple",
            "2. Apple",
            "1. Apple"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines.Count(l => l == "1. Apple"), Is.EqualTo(3));
        Assert.That(outputLines.Count(l => l == "2. Apple"), Is.EqualTo(1));
    }

    [Test]
    public async Task TestsSort_CaseSensitiveOrdering()
    {
        var inputLines = new[]
        {
            "1. apple",
            "2. Apple",
            "3. APPLE"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines[0], Is.EqualTo("3. APPLE"));
        Assert.That(outputLines[1], Is.EqualTo("2. Apple"));
        Assert.That(outputLines[2], Is.EqualTo("1. apple"));
    }

    [Test]
    public async Task TestsSort_ReportsProgress()
    {
        var inputLines = Enumerable.Range(1, 100)
            .Select(i => $"{i}. Line{i}")
            .ToArray();
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        var progressReports = new List<SortProgress>();
        var progress = new Progress<SortProgress>(p => progressReports.Add(p));

        await sorter.SortAsync(_inputFilePath, _outputFilePath, progress);
        await Task.Delay(100);

        Assert.That(progressReports.Count, Is.GreaterThan(0));
        Assert.That(progressReports.Any(p => p.Phase == SortPhase.CreatingChunks), Is.True);
        Assert.That(progressReports.Any(p => p.Phase == SortPhase.Completed), Is.True);
    }

    [Test]
    public async Task TestsSort_SpecialCharactersInText()
    {
        var inputLines = new[]
        {
            "1. Hello World!",
            "2. Test@123",
            "3. Alpha",
            "4. !Special"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        Assert.That(outputLines[0], Is.EqualTo("4. !Special"));
        Assert.That(outputLines.Length, Is.EqualTo(4));
    }

    [Test]
    public async Task TestsSort_LargeNumbers()
    {
        var inputLines = new[]
        {
            $"{long.MaxValue}. Max",
            "1. Min",
            $"{long.MaxValue / 2}. Half"
        };
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter();
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var outputLines = await File.ReadAllLinesAsync(_outputFilePath);
        _lineParser.TryParse(outputLines[0], out var first);
        _lineParser.TryParse(outputLines[1], out var second);
        _lineParser.TryParse(outputLines[2], out var third);

        Assert.That(first.Number, Is.EqualTo(long.MaxValue / 2));
        Assert.That(second.Number, Is.EqualTo(long.MaxValue));
        Assert.That(third.Number, Is.EqualTo(1));
    }

    [Test]
    public void TestsSort_NonExistentFile_ThrowsFileNotFoundException()
    {
        var sorter = CreateSorter();

        Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await sorter.SortAsync("nonexistent.txt", _outputFilePath));
    }

    [Test]
    public void TestsSort_Cancellation_ThrowsOperationCanceledException()
    {
        File.WriteAllLines(_inputFilePath, Enumerable.Range(1, 10000).Select(i => $"{i}. Line{i}"));

        var sorter = CreateSorter(maxMemory: 1024);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await sorter.SortAsync(_inputFilePath, _outputFilePath, cancellationToken: cts.Token));
    }

    [Test]
    public async Task TestsSort_CleansTempDirectory()
    {
        var inputLines = Enumerable.Range(1, 500)
            .Select(i => $"{i}. Line{i}")
            .ToArray();
        await File.WriteAllLinesAsync(_inputFilePath, inputLines);

        var sorter = CreateSorter(maxMemory: 5 * 1024, mergeWayCount: 4);
        await sorter.SortAsync(_inputFilePath, _outputFilePath);

        var tempDirs = Directory.GetDirectories(_testDirectory, ".fp_sort_temp_*");
        Assert.That(tempDirs, Is.Empty);
    }
}
