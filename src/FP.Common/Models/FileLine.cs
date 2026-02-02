using System.Text;

namespace FP.Common.Models;

/// <summary>
/// Represents a parsed line from the input file in format: "Number. Text"
/// </summary>
public readonly struct FileLine(long number, string text) : IEquatable<FileLine>
{
    public long Number { get; } = number;
    public string Text { get; } = text;

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

    /// <summary>
    /// Reconstructs the original line format "Number. Text"
    /// </summary>
    public string ToLineString() => $"{Number}. {Text}";

    public override string ToString() => ToLineString();

    /// <summary>
    /// Estimates the UTF-8 byte size of this line (for memory tracking).
    /// </summary>
    public int GetUtf8ByteCount()
    {
        // "Number. Text\n" - estimate number digits + separator + text + newline
        var numberDigits = Number switch
        {
            0 => 1,
            < 0 => 1 + (int)Math.Floor(Math.Log10(Math.Abs((double)Number))) + 1,
            _ => (int)Math.Floor(Math.Log10(Number)) + 1
        };
        return numberDigits + 2 + Encoding.UTF8.GetByteCount(Text) + Environment.NewLine.Length;
    }
}
