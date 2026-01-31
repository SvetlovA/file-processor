namespace FP.Generator.Providers;

public interface IRandomContentProvider
{
    string GenerateString(int minLength, int maxLength);
    long GenerateNumber(long min, long max);
    bool ShouldUseDuplicate(int duplicatePercentage);
}
