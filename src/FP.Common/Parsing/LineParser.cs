using FP.Common.Interfaces;
using FP.Common.Models;

namespace FP.Common.Parsing;

/// <summary>
/// Parses lines in the format "Number. Text" (e.g., "415. Apple")
/// </summary>
public class LineParser : ILineParser
{
    private const string Separator = ". ";

    public bool TryParse(string line, out FileLine result)
    {
        result = default;

        if (string.IsNullOrEmpty(line))
        {
            return false;
        }

        var dotIndex = line.IndexOf(Separator, StringComparison.Ordinal);
        if (dotIndex < 1)
        {
            return false;
        }

        var numberPart = line.AsSpan(0, dotIndex);
        if (!long.TryParse(numberPart, out long number))
        {
            return false;
        }

        var text = line.Substring(dotIndex + Separator.Length);
        result = new FileLine(number, text, line);
        return true;
    }

    public FileLine Parse(string line)
    {
        if (!TryParse(line, out FileLine result))
        {
            throw new FormatException($"Invalid line format: '{line}'. Expected format: 'Number. Text'");
        }
        return result;
    }
}
