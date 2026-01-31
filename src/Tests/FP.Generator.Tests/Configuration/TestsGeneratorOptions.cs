using FP.Generator.Configuration;
using NUnit.Framework;

namespace FP.Generator.Tests.Configuration;

[TestFixture]
public class TestsGeneratorOptions
{
    [TestCase("1024", 1024L)]
    [TestCase("1024B", 1024L)]
    [TestCase("1KB", 1024L)]
    [TestCase("1MB", 1024L * 1024)]
    [TestCase("1GB", 1024L * 1024 * 1024)]
    [TestCase("10MB", 10L * 1024 * 1024)]
    [TestCase("1.5GB", (long)(1.5 * 1024 * 1024 * 1024))]
    [TestCase("512kb", 512L * 1024)]
    [TestCase("  100MB  ", 100L * 1024 * 1024)]
    public void TestsParseSize_ValidInput_ReturnsCorrectBytes(string input, long expected)
    {
        var result = GeneratorOptions.ParseSize(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void TestsParseSize_InvalidInput_ThrowsException()
    {
        Assert.Throws<FormatException>(() => GeneratorOptions.ParseSize("invalid"));
    }

    [Test]
    public void TestsDefaultValues_AreSet()
    {
        var options = new GeneratorOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.TargetSizeBytes, Is.EqualTo(10 * 1024 * 1024));
            Assert.That(options.OutputPath, Is.EqualTo("output.txt"));
            Assert.That(options.DuplicatePercentage, Is.EqualTo(10));
            Assert.That(options.MinStringLength, Is.EqualTo(5));
            Assert.That(options.MaxStringLength, Is.EqualTo(50));
            Assert.That(options.MinNumber, Is.EqualTo(1));
            Assert.That(options.MaxNumber, Is.EqualTo(100_000_000));
            Assert.That(options.Seed, Is.Null);
        });
    }
}
