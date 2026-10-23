namespace MonteCarloPi.Core;

/// <summary>
/// An estimate of π from <see cref="Total"/> random points in the square [−1, 1]²,
/// of which <see cref="Inside"/> fell inside the unit circle: <c>π ≈ 4 · Inside / Total</c>.
/// </summary>
public readonly record struct PiEstimate(long Inside, long Total)
{
    /// <summary>z for a two-sided 95% interval.</summary>
    public const double Z95 = 1.959963984540054;

    public bool IsEmpty => Total == 0;

    public double Pi => Total == 0 ? double.NaN : 4.0 * Inside / Total;

    public double Error => Pi - Math.PI;

    /// <summary>
    /// Standard deviation of the estimate: each point is a Bernoulli trial with p = Inside / Total,
    /// so σ(π̂) = 4 · √(p(1 − p) / N).
    /// </summary>
    public double StandardError
    {
        get
        {
            if (Total == 0)
                return double.NaN;
            double p = (double)Inside / Total;
            return 4 * Math.Sqrt(p * (1 - p) / Total);
        }
    }

    /// <summary>The 95% confidence interval around <see cref="Pi"/>.</summary>
    public (double Lower, double Upper) Interval95 => (Pi - Z95 * StandardError, Pi + Z95 * StandardError);

    /// <summary>How many decimals of the estimate equal those of π (3.1418 → 3).</summary>
    public int MatchingDecimals => PiMath.MatchingDecimals(Pi);

    public PiEstimate Add(long inside, long total) => new(Inside + inside, Total + total);
}
