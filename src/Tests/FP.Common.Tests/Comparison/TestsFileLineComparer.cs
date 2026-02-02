using FP.Common.Comparison;
using FP.Common.Models;
using NUnit.Framework;

namespace FP.Common.Tests.Comparison;

[TestFixture]
public class TestsFileLineComparer
{
    private FileLineComparer _comparer = null!;

    [SetUp]
    public void SetUp()
    {
        _comparer = FileLineComparer.Instance;
    }

    [Test]
    public void TestsCompare_DifferentText_ReturnsTextComparison()
    {
        var apple = new FileLine(1, "Apple");
        var banana = new FileLine(1, "Banana");

        var result = _comparer.Compare(apple, banana);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void TestsCompare_SameTextDifferentNumber_ReturnsNumberComparison()
    {
        var apple1 = new FileLine(1, "Apple");
        var apple2 = new FileLine(2, "Apple");

        var result = _comparer.Compare(apple1, apple2);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void TestsCompare_SameTextSameNumber_ReturnsZero()
    {
        var line1 = new FileLine(1, "Apple");
        var line2 = new FileLine(1, "Apple");

        var result = _comparer.Compare(line1, line2);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void TestsCompare_TextFirstThenNumber_CorrectOrdering()
    {
        var lines = new List<FileLine>
        {
            new(2, "Banana"),
            new(1, "Apple"),
            new(3, "Apple"),
            new(2, "Apple"),
            new(1, "Banana"),
        };

        lines.Sort(_comparer);

        var expected = new List<(long Number, string Text)>
        {
            (1, "Apple"),
            (2, "Apple"),
            (3, "Apple"),
            (1, "Banana"),
            (2, "Banana"),
        };

        Assert.That(lines.Select(l => (l.Number, l.Text)), Is.EqualTo(expected));
    }

    [Test]
    public void TestsCompare_CaseSensitive_UppercaseBeforeLowercase()
    {
        var upper = new FileLine(1, "Apple");
        var lower = new FileLine(1, "apple");

        var result = _comparer.Compare(upper, lower);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void TestsCompare_EmptyText_ComparesCorrectly()
    {
        var empty = new FileLine(1, "");
        var nonEmpty = new FileLine(1, "Apple");

        var result = _comparer.Compare(empty, nonEmpty);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void TestsCompare_SpecialCharacters_ComparesCorrectly()
    {
        var special = new FileLine(1, "!Special");
        var alpha = new FileLine(1, "Alpha");

        var result = _comparer.Compare(special, alpha);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void TestsCompare_LargeNumbers_ComparesCorrectly()
    {
        var small = new FileLine(long.MinValue, "Test");
        var large = new FileLine(long.MaxValue, "Test");

        var result = _comparer.Compare(small, large);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void TestsCompare_ReversedComparison_ReturnsOppositeSign()
    {
        var apple = new FileLine(1, "Apple");
        var banana = new FileLine(1, "Banana");

        var result1 = _comparer.Compare(apple, banana);
        var result2 = _comparer.Compare(banana, apple);

        Assert.That(Math.Sign(result1), Is.EqualTo(-Math.Sign(result2)));
    }

    [Test]
    public void TestsCompare_WithPriorityQueue_CorrectDequeueOrder()
    {
        var queue = new PriorityQueue<FileLine, FileLine>(_comparer);

        queue.Enqueue(new FileLine(2, "Banana"), new FileLine(2, "Banana"));
        queue.Enqueue(new FileLine(1, "Apple"), new FileLine(1, "Apple"));
        queue.Enqueue(new FileLine(3, "Apple"), new FileLine(3, "Apple"));
        queue.Enqueue(new FileLine(2, "Apple"), new FileLine(2, "Apple"));

        var dequeued = new List<FileLine>();
        while (queue.Count > 0)
        {
            dequeued.Add(queue.Dequeue());
        }

        var expected = new List<(long Number, string Text)>
        {
            (1, "Apple"),
            (2, "Apple"),
            (3, "Apple"),
            (2, "Banana"),
        };

        Assert.That(dequeued.Select(l => (l.Number, l.Text)), Is.EqualTo(expected));
    }

    [Test]
    public void TestsInstance_ReturnsSameInstance()
    {
        var instance1 = FileLineComparer.Instance;
        var instance2 = FileLineComparer.Instance;

        Assert.That(instance1, Is.SameAs(instance2));
    }
}
