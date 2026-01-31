using FP.Generator.Providers;
using NUnit.Framework;

namespace FP.Generator.Tests.Services;

[TestFixture]
public class TestsRandomContentProvider
{
    [Test]
    public void TestsGenerateString_ReturnsStringWithinLengthRange()
    {
        var provider = new RandomContentProvider(seed: 42);

        for (int i = 0; i < 100; i++)
        {
            var result = provider.GenerateString(5, 10);
            Assert.That(result.Length, Is.InRange(5, 10));
        }
    }

    [Test]
    public void TestsGenerateString_WithSameSeed_ReturnsSameResults()
    {
        var provider1 = new RandomContentProvider(seed: 42);
        var provider2 = new RandomContentProvider(seed: 42);

        for (int i = 0; i < 10; i++)
        {
            var result1 = provider1.GenerateString(5, 10);
            var result2 = provider2.GenerateString(5, 10);
            Assert.That(result1, Is.EqualTo(result2));
        }
    }

    [Test]
    public void TestsGenerateString_WithMinEqualToMax_ReturnsExactLength()
    {
        var provider = new RandomContentProvider(seed: 42);

        var result = provider.GenerateString(7, 7);

        Assert.That(result.Length, Is.EqualTo(7));
    }

    [Test]
    public void TestsGenerateNumber_ReturnsNumberWithinRange()
    {
        var provider = new RandomContentProvider(seed: 42);

        for (int i = 0; i < 100; i++)
        {
            var result = provider.GenerateNumber(1, 1000);
            Assert.That(result, Is.InRange(1, 1000));
        }
    }

    [Test]
    public void TestsGenerateNumber_WithSameSeed_ReturnsSameResults()
    {
        var provider1 = new RandomContentProvider(seed: 42);
        var provider2 = new RandomContentProvider(seed: 42);

        for (int i = 0; i < 10; i++)
        {
            var result1 = provider1.GenerateNumber(1, 1000);
            var result2 = provider2.GenerateNumber(1, 1000);
            Assert.That(result1, Is.EqualTo(result2));
        }
    }

    [Test]
    public void TestsGenerateNumber_WithMinEqualToMax_ReturnsExactValue()
    {
        var provider = new RandomContentProvider(seed: 42);

        var result = provider.GenerateNumber(42, 42);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void TestsShouldUseDuplicate_With100Percent_AlwaysReturnsTrue()
    {
        var provider = new RandomContentProvider(seed: 42);

        for (int i = 0; i < 100; i++)
        {
            Assert.That(provider.ShouldUseDuplicate(100), Is.True);
        }
    }

    [Test]
    public void TestsShouldUseDuplicate_With0Percent_AlwaysReturnsFalse()
    {
        var provider = new RandomContentProvider(seed: 42);

        for (int i = 0; i < 100; i++)
        {
            Assert.That(provider.ShouldUseDuplicate(0), Is.False);
        }
    }

    [Test]
    public void TestsShouldUseDuplicate_With50Percent_ReturnsMixedResults()
    {
        var provider = new RandomContentProvider(seed: 42);
        int trueCount = 0;

        for (int i = 0; i < 1000; i++)
        {
            if (provider.ShouldUseDuplicate(50))
            {
                trueCount++;
            }
        }

        Assert.That(trueCount, Is.InRange(400, 600));
    }
}
