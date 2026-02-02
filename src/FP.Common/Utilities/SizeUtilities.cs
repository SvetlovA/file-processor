namespace FP.Common.Utilities;

/// <summary>
/// Utility methods for parsing and formatting byte sizes.
/// </summary>
public static class SizeUtilities
{
    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// Formats a byte count as a human-readable string (e.g., "1.50 GB").
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        var index = 0;
        double size = bytes;

        while (size >= 1024 && index < SizeSuffixes.Length - 1)
        {
            size /= 1024;
            index++;
        }

        return $"{size:F2} {SizeSuffixes[index]}";
    }

    /// <summary>
    /// Parses a size string (e.g., "100MB", "1.5GB") into bytes.
    /// </summary>
    /// <exception cref="FormatException">Thrown when the size string format is invalid.</exception>
    public static long ParseSize(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            throw new FormatException("Size cannot be empty.");
        }

        size = size.Trim().ToUpperInvariant();

        try
        {
            if (size.EndsWith("GB"))
            {
                return (long)(double.Parse(size[..^2]) * 1024 * 1024 * 1024);
            }
            if (size.EndsWith("MB"))
            {
                return (long)(double.Parse(size[..^2]) * 1024 * 1024);
            }
            if (size.EndsWith("KB"))
            {
                return (long)(double.Parse(size[..^2]) * 1024);
            }
            if (size.EndsWith("B"))
            {
                return long.Parse(size[..^1]);
            }

            return long.Parse(size);
        }
        catch (FormatException)
        {
            throw new FormatException($"Invalid size format: '{size}'. Expected format: number with optional suffix (B, KB, MB, GB). Examples: '100MB', '1.5GB', '1024'");
        }
        catch (OverflowException)
        {
            throw new FormatException($"Size value is too large: '{size}'.");
        }
    }
}
