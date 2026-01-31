using FP.Common.Interfaces;
using FP.Common.Models;

namespace FP.Common.Comparison;

/// <summary>
/// Compares FileLine instances by Text first (ordinal), then by Number (ascending).
/// </summary>
public class FileLineComparer : IComparer<FileLine>
{
    public static FileLineComparer Instance { get; } = new();

    public int Compare(FileLine x, FileLine y)
    {
        var textComparison = StringComparer.Ordinal.Compare(x.Text, y.Text);
        if (textComparison != 0)
        {
            return textComparison;
        }
        
        return x.Number.CompareTo(y.Number);
    }
}
