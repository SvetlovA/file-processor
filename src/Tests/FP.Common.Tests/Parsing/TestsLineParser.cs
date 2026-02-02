using FP.Common.Models;
using FP.Common.Parsing;
using NUnit.Framework;

namespace FP.Common.Tests.Parsing;

[TestFixture]
public class TestsLineParser
{
    private LineParser _parser = null!;

    [SetUp]
    public void SetUp()
    {
        _parser = new LineParser();
    }

    [Test]
    public void TestsParse_ValidLine_ReturnsCorrectFileLine()
    {
        const string line = "415. Apple";

        var result = _parser.Parse(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Number, Is.EqualTo(415));
            Assert.That(result.Text, Is.EqualTo("Apple"));
            Assert.That(result.ToLineString(), Is.EqualTo(line));
        });
    }

    [Test]
    public void TestsParse_LineWithLargeNumber_ReturnsCorrectFileLine()
    {
        const string line = "9223372036854775807. MaxLong";

        var result = _parser.Parse(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Number, Is.EqualTo(long.MaxValue));
            Assert.That(result.Text, Is.EqualTo("MaxLong"));
        });
    }

    [Test]
    public void TestsParse_LineWithZeroNumber_ReturnsCorrectFileLine()
    {
        const string line = "0. Zero";

        var result = _parser.Parse(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Number, Is.EqualTo(0));
            Assert.That(result.Text, Is.EqualTo("Zero"));
        });
    }

    [Test]
    public void TestsParse_LineWithTextContainingDots_ReturnsCorrectFileLine()
    {
        const string line = "123. Hello. World. Test";

        var result = _parser.Parse(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Number, Is.EqualTo(123));
            Assert.That(result.Text, Is.EqualTo("Hello. World. Test"));
        });
    }

    [Test]
    public void TestsParse_LineWithSpacesInText_ReturnsCorrectFileLine()
    {
        const string line = "42. Hello World Test";

        var result = _parser.Parse(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Number, Is.EqualTo(42));
            Assert.That(result.Text, Is.EqualTo("Hello World Test"));
        });
    }

    [Test]
    public void TestsParse_EmptyText_ReturnsCorrectFileLine()
    {
        const string line = "1. ";

        var result = _parser.Parse(line);

        Assert.Multiple(() =>
        {
            Assert.That(result.Number, Is.EqualTo(1));
            Assert.That(result.Text, Is.EqualTo(""));
        });
    }

    [Test]
    public void TestsParse_InvalidFormat_NoDotSpace_ThrowsFormatException()
    {
        const string line = "415 Apple";

        Assert.Throws<FormatException>(() => _parser.Parse(line));
    }

    [Test]
    public void TestsParse_InvalidFormat_NoNumber_ThrowsFormatException()
    {
        const string line = ". Apple";

        Assert.Throws<FormatException>(() => _parser.Parse(line));
    }

    [Test]
    public void TestsParse_InvalidFormat_NonNumericNumber_ThrowsFormatException()
    {
        const string line = "abc. Apple";

        Assert.Throws<FormatException>(() => _parser.Parse(line));
    }

    [Test]
    public void TestsParse_EmptyLine_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => _parser.Parse(""));
    }

    [Test]
    public void TestsParse_NullLine_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => _parser.Parse(null!));
    }

    [Test]
    public void TestsTryParse_ValidLine_ReturnsTrueAndFileLine()
    {
        const string line = "415. Apple";

        var success = _parser.TryParse(line, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result.Number, Is.EqualTo(415));
            Assert.That(result.Text, Is.EqualTo("Apple"));
        });
    }

    [Test]
    public void TestsTryParse_InvalidFormat_ReturnsFalse()
    {
        const string line = "invalid line";

        var success = _parser.TryParse(line, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(default(FileLine)));
        });
    }

    [Test]
    public void TestsTryParse_EmptyLine_ReturnsFalse()
    {
        var success = _parser.TryParse("", out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TestsTryParse_NullLine_ReturnsFalse()
    {
        var success = _parser.TryParse(null!, out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void TestsTryParse_NegativeNumber_ReturnsCorrectFileLine()
    {
        const string line = "-123. Negative";

        var success = _parser.TryParse(line, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result.Number, Is.EqualTo(-123));
            Assert.That(result.Text, Is.EqualTo("Negative"));
        });
    }
}
