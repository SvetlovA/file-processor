using FP.Common.Parsing;
using FP.Sorter.Sorters;
using NUnit.Framework;

namespace FP.Sorter.Tests.Services;

[TestFixture]
public class TestsChunkSorter
{
    private string _testDirectory = null!;
    private string _inputFilePath = null!;
    private string _tempDirectory = null!;
    private LineParser _lineParser = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FP.Sorter.Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _inputFilePath = Path.Combine(_testDirectory, "input.txt");
        _tempDirectory = Path.Combine(_testDirectory, "temp");
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

    [Test]
    public async Task TestsCreateSortedChunksAsync_CreatesChunkFiles()
    {
        await File.WriteAllLinesAsync(_inputFilePath, new[]
        {
            "3. Apple",
            "1. Banana",
            "2. Cherry"
        });
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 1024 * 1024);

        var chunks = await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory);

        Assert.That(chunks.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(chunks.All(File.Exists), Is.True);
    }

    [Test]
    public async Task TestsCreateSortedChunksAsync_ChunksAreSorted()
    {
        await File.WriteAllLinesAsync(_inputFilePath, new[]
        {
            "3. Cherry",
            "1. Apple",
            "2. Banana"
        });
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 1024 * 1024);

        var chunks = await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(chunks[0]);
        var expected = new[] { "1. Apple", "2. Banana", "3. Cherry" };
        Assert.That(lines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsCreateSortedChunksAsync_SortsByTextThenNumber()
    {
        await File.WriteAllLinesAsync(_inputFilePath, new[]
        {
            "2. Apple",
            "1. Banana",
            "3. Apple",
            "1. Apple"
        });
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 1024 * 1024);

        var chunks = await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(chunks[0]);
        var expected = new[] { "1. Apple", "2. Apple", "3. Apple", "1. Banana" };
        Assert.That(lines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsCreateSortedChunksAsync_SmallChunkSize_CreatesMultipleChunks()
    {
        var lines = Enumerable.Range(1, 100)
            .Select(i => $"{i}. Line{i:D3}")
            .ToArray();

        await File.WriteAllLinesAsync(_inputFilePath, lines);
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 500);

        var chunks = await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory);

        Assert.That(chunks.Count, Is.GreaterThan(1));
    }

    [Test]
    public async Task TestsCreateSortedChunksAsync_ReportsProgress()
    {
        var lines = Enumerable.Range(1, 50)
            .Select(i => $"{i}. Line{i}")
            .ToArray();

        await File.WriteAllLinesAsync(_inputFilePath, lines);
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 1024 * 1024);
        var progressReports = new List<ChunkProgress>();

        var progress = new Progress<ChunkProgress>(p => progressReports.Add(p));

        await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory, progress);
        await Task.Delay(100);

        Assert.That(progressReports.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task TestsCreateSortedChunksAsync_EmptyFile_ReturnsNoChunks()
    {
        await File.WriteAllTextAsync(_inputFilePath, string.Empty);

        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 1024 * 1024);

        var chunks = await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory);

        Assert.That(chunks, Is.Empty);
    }

    [Test]
    public async Task TestsCreateSortedChunksAsync_InvalidLines_AreSkipped()
    {
        await File.WriteAllLinesAsync(_inputFilePath, new[]
        {
            "1. Valid",
            "invalid line",
            "2. AlsoValid",
            "another invalid"
        });

        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 1024 * 1024);

        var chunks = await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(chunks[0]);
        Assert.That(lines.Length, Is.EqualTo(2));
    }

    [Test]
    public void TestsCreateSortedChunksAsync_Cancellation_ThrowsOperationCanceledException()
    {
        File.WriteAllLines(_inputFilePath, Enumerable.Range(1, 1000).Select(i => $"{i}. Line{i}"));
        
        var chunkSorter = new ChunkSorter(_lineParser, chunkSizeBytes: 100);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await chunkSorter.CreateSortedChunksAsync(_inputFilePath, _tempDirectory, cancellationToken: cts.Token));
    }
}
