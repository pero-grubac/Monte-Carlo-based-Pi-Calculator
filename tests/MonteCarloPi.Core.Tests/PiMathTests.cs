using MonteCarloPi.Core;

namespace MonteCarloPi.Core.Tests;

public class PiMathTests
{
    [Theory]
    [InlineData(3.1418, 3)]
    [InlineData(3.14159, 5)]
    [InlineData(3.2, 0)]
    [InlineData(3.0, 0)]
    [InlineData(2.99, 0)]
    [InlineData(3.141592653589793, 15)]
    [InlineData(double.NaN, 0)]
    public void MatchingDecimals_CountsLeadingDecimals(double estimate, int expected)
    {
        Assert.Equal(expected, PiMath.MatchingDecimals(estimate));
    }

    [Theory]
    [InlineData(0, 3.0, 4.0)]
    [InlineData(2, 3.14, 3.15)]
    [InlineData(4, 3.1415, 3.1416)]
    public void DecimalWindow_IsTruncationInterval(int decimals, double lower, double upper)
    {
        var (l, u) = PiMath.DecimalWindow(decimals);

        Assert.Equal(lower, l, 12);
        Assert.Equal(upper, u, 12);
    }

    [Fact]
    public void SamplesForDecimals_GrowsHundredfoldPerDecimal()
    {
        // N = (1.96 · 1.642 / 0.005)² ≈ 414,000 for two decimals.
        Assert.InRange(PiMath.SamplesForDecimals(2), 413_500, 414_500);
        Assert.Equal(100, PiMath.SamplesForDecimals(4) / PiMath.SamplesForDecimals(3), 6);
    }

    [Fact]
    public void Estimate_ComputesPiAndStandardError()
    {
        var estimate = new PiEstimate(Inside: 7854, Total: 10_000);

        Assert.Equal(3.1416, estimate.Pi, 12);
        Assert.Equal(4 * Math.Sqrt(0.7854 * 0.2146 / 10_000), estimate.StandardError, 12);
        Assert.Equal(estimate.Pi - PiEstimate.Z95 * estimate.StandardError, estimate.Interval95.Lower, 12);
        Assert.Equal(3, estimate.MatchingDecimals);   // 3.1416 vs 3.14159…
    }

    [Fact]
    public void EmptyEstimate_IsNaN()
    {
        var estimate = default(PiEstimate);

        Assert.True(estimate.IsEmpty);
        Assert.True(double.IsNaN(estimate.Pi));
        Assert.Equal(0, estimate.MatchingDecimals);
    }
}
