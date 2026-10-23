using MonteCarloPi.Core;

namespace MonteCarloPi.Core.Tests;

public class PiSimulationTests
{
    [Fact]
    public void Run_IsCloseToPi()
    {
        var estimate = PiSimulation.Run(4_000_000, seed: 42);

        // Within 5 standard errors: fails by chance less than once in a million seeds.
        Assert.InRange(estimate.Pi, Math.PI - 5 * estimate.StandardError, Math.PI + 5 * estimate.StandardError);
        Assert.Equal(4_000_000, estimate.Total);
    }

    [Fact]
    public void SameSeed_GivesSameResult()
    {
        Assert.Equal(PiSimulation.Run(100_000, 7), PiSimulation.Run(100_000, 7));
    }

    [Fact]
    public void DifferentSeeds_GiveDifferentResults()
    {
        Assert.NotEqual(PiSimulation.Run(100_000, 1), PiSimulation.Run(100_000, 2));
    }

    [Theory]
    [InlineData(new long[] { 3_000_000 })]
    [InlineData(new long[] { 1, 999, 500_000, 2_497_000, 2_000 })]
    [InlineData(new long[] { PiSimulation.ChunkSize, PiSimulation.ChunkSize, 3_000_000 - 2 * PiSimulation.ChunkSize })]
    [InlineData(new long[] { 1_048_575, 2, 1_951_423 })]
    public void Result_DoesNotDependOnHowWorkIsSplit(long[] batches)
    {
        var expected = PiSimulation.Run(3_000_000, seed: 99);
        var simulation = new PiSimulation(99);

        foreach (long batch in batches)
            simulation.Run(batch);

        Assert.Equal(expected, simulation.Estimate);
    }

    [Fact]
    public void Capture_ReturnsTheFirstPoints()
    {
        var simulation = new PiSimulation(5);
        var points = new SamplePoint[100];

        int captured = simulation.Run(50, points);

        Assert.Equal(50, captured);
        Assert.Equal(simulation.Estimate.Inside, points.Take(50).Count(p => p.Inside));
        Assert.All(points.Take(50), p =>
        {
            Assert.InRange(p.X, -1f, 1f);
            Assert.InRange(p.Y, -1f, 1f);
            float r2 = p.X * p.X + p.Y * p.Y;
            if (Math.Abs(r2 - 1) > 1e-5)      // stored as float: skip points right on the circle
                Assert.Equal(r2 < 1, p.Inside);
        });
    }

    [Fact]
    public void Capture_DoesNotChangeTheResult()
    {
        var withCapture = new PiSimulation(11);
        withCapture.Run(2_500_000, new SamplePoint[5000]);

        Assert.Equal(PiSimulation.Run(2_500_000, 11), withCapture.Estimate);
    }

    [Fact]
    public void RunUntilDecimalsMatch_StopsInsideTheWindow()
    {
        var simulation = new PiSimulation(3);

        var (matched, _) = simulation.RunUntilDecimalsMatch(2, budget: 10_000_000);

        Assert.True(matched);
        Assert.InRange(simulation.Estimate.Pi, 3.14, 3.15);
        Assert.True(simulation.Estimate.MatchingDecimals >= 2);
    }

    [Fact]
    public void RunUntilDecimalsMatch_StopsAtTheFirstMatchingPoint()
    {
        var simulation = new PiSimulation(3);
        simulation.RunUntilDecimalsMatch(2, budget: 10_000_000);
        long total = simulation.Estimate.Total;

        // Replaying one point at a time, no earlier prefix may already have matched.
        var replay = new PiSimulation(3);
        for (long i = 1; i < total; i++)
        {
            replay.Run(1);
            Assert.False(replay.Estimate.Pi is >= 3.14 and < 3.15, $"Already matched after {i} points");
        }
    }

    [Fact]
    public void RunUntilDecimalsMatch_RespectsBudget()
    {
        var simulation = new PiSimulation(1);

        var (matched, _) = simulation.RunUntilDecimalsMatch(9, budget: 1000);

        Assert.False(matched);
        Assert.Equal(1000, simulation.Estimate.Total);
    }

    [Fact]
    public void RunUntilDecimalsMatch_ContinuesTheSameStream()
    {
        var a = new PiSimulation(8);
        a.RunUntilDecimalsMatch(12, budget: 1_500_000);
        a.Run(600_000);

        Assert.Equal(PiSimulation.Run(2_100_000, 8), a.Estimate);
    }

    [Fact]
    public void Run_CanBeCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => new PiSimulation(1).Run(50L * PiSimulation.ChunkSize, default, cts.Token));
    }

    [Fact]
    public void Xoshiro_IsUniform()
    {
        var rng = new Xoshiro256(123);
        var buckets = new int[10];
        const int n = 1_000_000;
        for (int i = 0; i < n; i++)
            buckets[(int)(rng.NextDouble() * 10)]++;

        Assert.All(buckets, count => Assert.InRange(count, n / 10 * 0.98, n / 10 * 1.02));
    }
}
