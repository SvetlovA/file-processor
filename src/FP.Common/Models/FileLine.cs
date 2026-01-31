namespace FP.Common.Models;

/// <summary>
/// Represents a parsed line from the input file in format: "Number. Text"
/// </summary>
public readonly struct FileLine(long number, string text, string originalLine) : IEquatable<FileLine>
{
    public long Number { get; } = number;
    public string Text { get; } = text;
    public string OriginalLine { get; } = originalLine;

    public bool Equals(FileLine other)
    {
        return Number == other.Number && Text == other.Text;
    }

    public override bool Equals(object? obj)
    {
        return obj is FileLine other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Number, Text);
    }

    public static bool operator ==(FileLine left, FileLine right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FileLine left, FileLine right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return OriginalLine;
    }
}
