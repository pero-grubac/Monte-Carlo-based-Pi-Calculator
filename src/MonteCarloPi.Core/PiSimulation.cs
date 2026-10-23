namespace MonteCarloPi.Core;

/// <summary>A random point in [−1, 1]² and whether it fell inside the unit circle.</summary>
public readonly record struct SamplePoint(float X, float Y, bool Inside);

/// <summary>
/// Throws random points at the square [−1, 1]² and counts those inside the unit circle.
/// </summary>
/// <remarks>
/// Point number <c>i</c> always comes from the same random stream (chunk <c>i / ChunkSize</c> of the seed),
/// so a seed gives the same result however the work is split into calls and however many threads run it.
/// </remarks>
public sealed class PiSimulation
{
    /// <summary>Points per independent random stream; also the unit of parallel work.</summary>
    public const int ChunkSize = 1 << 20;

    private Xoshiro256 _rng;
    private long _rngChunk = -1;

    public PiSimulation(ulong seed)
    {
        Seed = seed;
    }

    /// <summary>A simulation with a seed from the system's random generator.</summary>
    public PiSimulation()
        : this((ulong)Random.Shared.NextInt64())
    {
    }

    public ulong Seed { get; }

    public PiEstimate Estimate { get; private set; }

    /// <summary>Estimates π from <paramref name="samples"/> points.</summary>
    public static PiEstimate Run(long samples, ulong seed, CancellationToken cancellationToken = default)
    {
        var simulation = new PiSimulation(seed);
        simulation.Run(samples, default, cancellationToken);
        return simulation.Estimate;
    }

    /// <summary>
    /// Draws <paramref name="count"/> more points, using every core for large counts.
    /// The first points are copied into <paramref name="capture"/>, for drawing.
    /// </summary>
    /// <returns>How many points were copied into <paramref name="capture"/>.</returns>
    public int Run(long count, Span<SamplePoint> capture = default, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        // Finish the chunk in progress, and the first full chunk too if points are still to be captured.
        long offset = Estimate.Total % ChunkSize;
        long head = Math.Min(count, offset == 0 ? 0 : ChunkSize - offset);
        if (capture.Length > head)
            head = Math.Min(count, head + ChunkSize);

        var (_, _, captured, _) = Sequential(head, capture, window: null);
        long remaining = count - head;

        // Whole chunks in parallel, each from its own stream.
        long firstChunk = Estimate.Total / ChunkSize;
        long chunks = remaining / ChunkSize;
        if (chunks > 0)
        {
            long inside = 0;
            Parallel.For(0, chunks, new ParallelOptions { CancellationToken = cancellationToken },
                () => 0L,
                (i, _, local) =>
                {
                    var rng = new Xoshiro256(Xoshiro256.StreamSeed(Seed, firstChunk + i));
                    return local + CountInside(ref rng, ChunkSize);
                },
                local => Interlocked.Add(ref inside, local));

            Estimate = Estimate.Add(inside, chunks * ChunkSize);
            remaining -= chunks * ChunkSize;
        }

        cancellationToken.ThrowIfCancellationRequested();
        Sequential(remaining, default, window: null);
        return captured;
    }

    /// <summary>
    /// Draws points one at a time until the estimate's first <paramref name="decimals"/> decimals equal π's,
    /// or until <paramref name="budget"/> points have been drawn.
    /// </summary>
    /// <returns>Whether the decimals match now, and how many points were copied into <paramref name="capture"/>.</returns>
    public (bool Matched, int Captured) RunUntilDecimalsMatch(int decimals, long budget, Span<SamplePoint> capture = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(budget);
        var window = PiMath.DecimalWindow(decimals);
        if (InWindow(Estimate.Inside, Estimate.Total, window))
            return (true, 0);

        var (_, _, captured, matched) = Sequential(budget, capture, window);
        return (matched, captured);
    }

    /// <summary>
    /// Draws up to <paramref name="count"/> points on this thread, crossing chunk boundaries as needed.
    /// With a <paramref name="window"/>, stops at the first point after which the estimate lies inside it.
    /// </summary>
    private (long Inside, long Drawn, int Captured, bool Matched) Sequential(long count, Span<SamplePoint> capture, (double Lower, double Upper)? window)
    {
        long inside = 0, drawn = 0;
        int captured = 0;
        bool matched = false;

        while (drawn < count && !matched)
        {
            long position = Estimate.Total + drawn;
            long chunk = position / ChunkSize;
            if (_rngChunk != chunk)
            {
                _rng = new Xoshiro256(Xoshiro256.StreamSeed(Seed, chunk));
                _rngChunk = chunk;
            }

            long n = Math.Min(count - drawn, ChunkSize - position % ChunkSize);
            for (long i = 0; i < n; i++)
            {
                double x = 2 * _rng.NextDouble() - 1;
                double y = 2 * _rng.NextDouble() - 1;
                bool isInside = x * x + y * y <= 1;
                if (isInside)
                    inside++;
                drawn++;

                if (captured < capture.Length)
                    capture[captured++] = new SamplePoint((float)x, (float)y, isInside);

                if (window is { } w && InWindow(Estimate.Inside + inside, Estimate.Total + drawn, w))
                {
                    matched = true;
                    break;
                }
            }
        }

        Estimate = Estimate.Add(inside, drawn);
        return (inside, drawn, captured, matched);
    }

    private static long CountInside(ref Xoshiro256 rng, long count)
    {
        long inside = 0;
        for (long i = 0; i < count; i++)
        {
            double x = 2 * rng.NextDouble() - 1;
            double y = 2 * rng.NextDouble() - 1;
            if (x * x + y * y <= 1)
                inside++;
        }
        return inside;
    }

    // lower ≤ 4·inside/total < upper, without dividing.
    private static bool InWindow(long inside, long total, (double Lower, double Upper) window) =>
        total > 0 && 4.0 * inside >= window.Lower * total && 4.0 * inside < window.Upper * total;
}
