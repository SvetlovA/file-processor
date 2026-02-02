using FP.Common.Parsing;
using FP.Sorter.Mergers;
using NUnit.Framework;

namespace FP.Sorter.Tests.Services;

[TestFixture]
public class TestsKWayMerger
{
    private string _testDirectory = null!;
    private string _tempDirectory = null!;
    private LineParser _lineParser = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FP.Sorter.Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _tempDirectory = Path.Combine(_testDirectory, "temp");
        Directory.CreateDirectory(_tempDirectory);
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
    public async Task TestsMergeChunksAsync_TwoChunks_MergesCorrectly()
    {
        var chunk1Path = Path.Combine(_testDirectory, "chunk1.txt");
        var chunk2Path = Path.Combine(_testDirectory, "chunk2.txt");
        var outputPath = Path.Combine(_testDirectory, "output.txt");

        await File.WriteAllLinesAsync(chunk1Path, new[] { "1. Apple", "3. Cherry" });
        await File.WriteAllLinesAsync(chunk2Path, new[] { "2. Banana", "4. Date" });

        var merger = new KWayMerger(16, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(new[] { chunk1Path, chunk2Path }, outputPath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(outputPath);
        var expected = new[] { "1. Apple", "2. Banana", "3. Cherry", "4. Date" };
        Assert.That(lines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsMergeChunksAsync_MergesSameTextByNumber()
    {
        var chunk1Path = Path.Combine(_testDirectory, "chunk1.txt");
        var chunk2Path = Path.Combine(_testDirectory, "chunk2.txt");
        var outputPath = Path.Combine(_testDirectory, "output.txt");

        await File.WriteAllLinesAsync(chunk1Path, new[] { "1. Apple", "3. Apple" });
        await File.WriteAllLinesAsync(chunk2Path, new[] { "2. Apple", "4. Apple" });

        var merger = new KWayMerger(16, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(new[] { chunk1Path, chunk2Path }, outputPath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(outputPath);
        var expected = new[] { "1. Apple", "2. Apple", "3. Apple", "4. Apple" };
        Assert.That(lines, Is.EqualTo(expected));
    }

    [Test]
    public async Task TestsMergeChunksAsync_ManyChunks_MergesCorrectly()
    {
        var chunkPaths = new List<string>();
        var allLines = new List<(int Number, string Text, string Line)>();

        for (int i = 0; i < 5; i++)
        {
            var chunkPath = Path.Combine(_testDirectory, $"chunk{i}.txt");
            var chunkLines = new[]
            {
                $"{i * 10 + 1}. Text{i}A",
                $"{i * 10 + 2}. Text{i}B"
            };
            await File.WriteAllLinesAsync(chunkPath, chunkLines);
            chunkPaths.Add(chunkPath);

            foreach (var line in chunkLines)
            {
                _lineParser.TryParse(line, out var fl);
                allLines.Add(((int)fl.Number, fl.Text, fl.ToLineString()));
            }
        }

        var outputPath = Path.Combine(_testDirectory, "output.txt");
        var merger = new KWayMerger(16, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(chunkPaths, outputPath, _tempDirectory);

        var resultLines = await File.ReadAllLinesAsync(outputPath);
        var expectedLines = allLines.OrderBy(x => x.Text).ThenBy(x => x.Number).Select(x => x.Line).ToArray();

        Assert.That(resultLines, Is.EqualTo(expectedLines));
    }

    [Test]
    public async Task TestsMergeChunksAsync_SingleChunk_MovesToOutput()
    {
        var chunkPath = Path.Combine(_testDirectory, "chunk.txt");
        var outputPath = Path.Combine(_testDirectory, "output.txt");

        await File.WriteAllLinesAsync(chunkPath, new[] { "1. Apple", "2. Banana" });

        var merger = new KWayMerger(16, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(new[] { chunkPath }, outputPath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(outputPath);
        Assert.That(lines, Is.EqualTo(new[] { "1. Apple", "2. Banana" }));
    }

    [Test]
    public async Task TestsMergeChunksAsync_NoChunks_CreatesEmptyOutput()
    {
        var outputPath = Path.Combine(_testDirectory, "output.txt");

        var merger = new KWayMerger(16, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(Array.Empty<string>(), outputPath, _tempDirectory);

        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(await File.ReadAllTextAsync(outputPath), Is.Empty);
    }

    [Test]
    public async Task TestsMergeChunksAsync_ReportsProgress()
    {
        var chunkPaths = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var chunkPath = Path.Combine(_testDirectory, $"chunk{i}.txt");
            await File.WriteAllLinesAsync(chunkPath, new[] { $"{i}. Text{i}" });
            chunkPaths.Add(chunkPath);
        }

        var outputPath = Path.Combine(_testDirectory, "output.txt");
        var progressReports = new List<MergeProgress>();
        var progress = new Progress<MergeProgress>(p => progressReports.Add(p));

        var merger = new KWayMerger(2, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(chunkPaths, outputPath, _tempDirectory, progress);
        await Task.Delay(100);

        Assert.That(progressReports.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task TestsMergeChunksAsync_DeletesTempFiles()
    {
        var chunkPaths = new List<string>();
        for (int i = 0; i < 4; i++)
        {
            var chunkPath = Path.Combine(_testDirectory, $"chunk{i}.txt");
            await File.WriteAllLinesAsync(chunkPath, new[] { $"{i}. Text{i}" });
            chunkPaths.Add(chunkPath);
        }

        var outputPath = Path.Combine(_testDirectory, "output.txt");
        var merger = new KWayMerger(2, _lineParser, deleteTempFiles: true);

        await merger.MergeChunksAsync(chunkPaths, outputPath, _tempDirectory);

        foreach (var chunkPath in chunkPaths)
        {
            Assert.That(File.Exists(chunkPath), Is.False);
        }
    }

    [Test]
    public async Task TestsMergeChunksAsync_MultiplePassesMerge()
    {
        var chunkPaths = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var chunkPath = Path.Combine(_testDirectory, $"chunk{i}.txt");
            await File.WriteAllLinesAsync(chunkPath, new[] { $"{i}. Text{(char)('A' + i)}" });
            chunkPaths.Add(chunkPath);
        }

        var outputPath = Path.Combine(_testDirectory, "output.txt");
        var merger = new KWayMerger(4, _lineParser, deleteTempFiles: false);

        await merger.MergeChunksAsync(chunkPaths, outputPath, _tempDirectory);

        var lines = await File.ReadAllLinesAsync(outputPath);
        Assert.That(lines.Length, Is.EqualTo(10));

        for (int i = 1; i < lines.Length; i++)
        {
            _lineParser.TryParse(lines[i - 1], out var prev);
            _lineParser.TryParse(lines[i], out var curr);
            Assert.That(string.Compare(prev.Text, curr.Text, StringComparison.Ordinal), Is.LessThanOrEqualTo(0));
        }
    }

    [Test]
    public void TestsMergeChunksAsync_Cancellation_ThrowsOperationCanceledException()
    {
        var chunkPaths = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            var chunkPath = Path.Combine(_testDirectory, $"chunk{i}.txt");
            File.WriteAllLines(chunkPath, Enumerable.Range(0, 100).Select(j => $"{j}. Text{i}_{j}"));
            chunkPaths.Add(chunkPath);
        }

        var outputPath = Path.Combine(_testDirectory, "output.txt");
        var merger = new KWayMerger(2, _lineParser);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await merger.MergeChunksAsync(chunkPaths, outputPath, _tempDirectory, cancellationToken: cts.Token));
    }
}
