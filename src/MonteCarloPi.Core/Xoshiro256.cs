namespace MonteCarloPi.Core;

/// <summary>
/// xoshiro256** pseudo-random generator: fast, statistically strong, and reproducible from a seed
/// (unlike <see cref="Random"/>, whose seeded algorithm is an old, slower one kept for compatibility).
/// </summary>
/// <remarks>A mutable struct: pass it by <see langword="ref"/>, never by value.</remarks>
public struct Xoshiro256
{
    private ulong _s0, _s1, _s2, _s3;

    /// <summary>Seeds the four state words from one value with SplitMix64, as the authors recommend.</summary>
    public Xoshiro256(ulong seed)
    {
        _s0 = SplitMix64(ref seed);
        _s1 = SplitMix64(ref seed);
        _s2 = SplitMix64(ref seed);
        _s3 = SplitMix64(ref seed);
    }

    /// <summary>A seed for the <paramref name="index"/>-th independent stream derived from <paramref name="seed"/>.</summary>
    public static ulong StreamSeed(ulong seed, long index)
    {
        ulong x = seed ^ (0x9E3779B97F4A7C15UL * (ulong)(index + 1));
        return SplitMix64(ref x);
    }

    public ulong NextUInt64()
    {
        ulong result = RotateLeft(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = RotateLeft(_s3, 45);

        return result;
    }

    /// <summary>A uniform double in [0, 1) with 53 random bits.</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    private static ulong RotateLeft(ulong x, int k) => (x << k) | (x >> (64 - k));

    private static ulong SplitMix64(ref ulong x)
    {
        ulong z = x += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
