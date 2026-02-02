using FP.Common.Utilities;
using NUnit.Framework;

namespace FP.Common.Tests.Utilities;

[TestFixture]
public class TestsSizeUtilities
{
    [TestCase("100B", 100L)]
    [TestCase("1KB", 1024L)]
    [TestCase("1MB", 1024L * 1024)]
    [TestCase("1GB", 1024L * 1024 * 1024)]
    [TestCase("1.5GB", (long)(1.5 * 1024 * 1024 * 1024))]
    [TestCase("512MB", 512L * 1024 * 1024)]
    [TestCase("100", 100L)]
    [TestCase("  100MB  ", 100L * 1024 * 1024)]
    [TestCase("100mb", 100L * 1024 * 1024)]
    public void TestsParseSize_ValidInput_ReturnsCorrectBytes(string input, long expected)
    {
        var result = SizeUtilities.ParseSize(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void TestsParseSize_EmptyOrNull_ThrowsFormatException(string? input)
    {
        Assert.Throws<FormatException>(() => SizeUtilities.ParseSize(input!));
    }

    [TestCase("abc")]
    [TestCase("10XB")]
    [TestCase("MB100")]
    [TestCase("--100MB")]
    public void TestsParseSize_InvalidFormat_ThrowsFormatExceptionWithMessage(string input)
    {
        var ex = Assert.Throws<FormatException>(() => SizeUtilities.ParseSize(input));
        Assert.That(ex!.Message, Does.Contain("Invalid size format"));
    }

    [TestCase(0L, "0.00 B")]
    [TestCase(100L, "100.00 B")]
    [TestCase(1024L, "1.00 KB")]
    [TestCase(1024L * 1024, "1.00 MB")]
    [TestCase(1024L * 1024 * 1024, "1.00 GB")]
    [TestCase(1536L, "1.50 KB")]
    [TestCase(1024L * 1024 * 1024 * 2, "2.00 GB")]
    public void TestsFormatBytes_ReturnsHumanReadableString(long bytes, string expected)
    {
        var result = SizeUtilities.FormatBytes(bytes);
        Assert.That(result, Is.EqualTo(expected));
    }
}
