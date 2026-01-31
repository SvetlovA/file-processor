using FP.Common.Models;

namespace FP.Common.Interfaces;

/// <summary>
/// Interface for parsing lines in the format "Number. Text"
/// </summary>
public interface ILineParser
{
    /// <summary>
    /// Attempts to parse a line into a FileLine structure.
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <param name="result">The parsed FileLine if successful</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    bool TryParse(string line, out FileLine result);

    /// <summary>
    /// Parses a line into a FileLine structure.
    /// </summary>
    /// <param name="line">The line to parse</param>
    /// <returns>The parsed FileLine</returns>
    /// <exception cref="FormatException">Thrown when the line format is invalid</exception>
    FileLine Parse(string line);
}
