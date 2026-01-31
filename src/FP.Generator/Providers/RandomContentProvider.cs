namespace FP.Generator.Providers;

public class RandomContentProvider(int? seed = null) : IRandomContentProvider
{
    private readonly Random _random = seed.HasValue ? new Random(seed.Value) : new Random();
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";

    public string GenerateString(int minLength, int maxLength)
    {
        var length = _random.Next(minLength, maxLength + 1);
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = Chars[_random.Next(Chars.Length)];
        }

        return new string(chars);
    }

    public long GenerateNumber(long min, long max)
    {
        return _random.NextInt64(min, max + 1);
    }

    public bool ShouldUseDuplicate(int duplicatePercentage)
    {
        return _random.Next(100) < duplicatePercentage;
    }
}
