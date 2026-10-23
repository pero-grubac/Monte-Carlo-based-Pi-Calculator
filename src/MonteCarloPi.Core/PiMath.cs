namespace MonteCarloPi.Core;

/// <summary>Facts about estimating π by sampling, independent of any particular run.</summary>
public static class PiMath
{
    /// <summary>The digits of π shown and compared against (15 decimals, the precision of a double).</summary>
    public const string Digits = "3.141592653589793";

    /// <summary>Standard deviation of a single point's contribution: √(π(4 − π)) ≈ 1.642.</summary>
    public static readonly double SingleSampleDeviation = Math.Sqrt(Math.PI * (4 - Math.PI));

    public const int MaxDecimals = 15;

    /// <summary>How many leading decimals of <paramref name="estimate"/> equal those of π, by truncation.</summary>
    public static int MatchingDecimals(double estimate)
    {
        if (!double.IsFinite(estimate) || Math.Floor(estimate) != 3)
            return 0;

        string text = estimate.ToString("F" + MaxDecimals, System.Globalization.CultureInfo.InvariantCulture);
        int matching = 0;
        for (int i = 2; i < Digits.Length && i < text.Length && text[i] == Digits[i]; i++)
            matching++;
        return matching;
    }

    /// <summary>
    /// The interval [lower, upper) of estimates whose first <paramref name="decimals"/> decimals are those of π,
    /// for example [3.14, 3.15) for two decimals.
    /// </summary>
    public static (double Lower, double Upper) DecimalWindow(int decimals)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimals);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimals, MaxDecimals);

        decimal lower = decimal.Parse(Digits[..(2 + decimals)].TrimEnd('.'), System.Globalization.CultureInfo.InvariantCulture);
        decimal step = 1m / (decimal)Math.Pow(10, decimals);
        return ((double)lower, (double)(lower + step));
    }

    /// <summary>
    /// Points needed for a 95% interval no wider than ± <paramref name="halfWidth"/>:
    /// N = (z · σ₁ / halfWidth)².
    /// </summary>
    public static double SamplesForHalfWidth(double halfWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(halfWidth);
        double n = PiEstimate.Z95 * SingleSampleDeviation / halfWidth;
        return n * n;
    }

    /// <summary>Points needed to be 95% sure that the first <paramref name="decimals"/> decimals are right (error below half a unit).</summary>
    public static double SamplesForDecimals(int decimals) => SamplesForHalfWidth(0.5 * Math.Pow(10, -decimals));
}
