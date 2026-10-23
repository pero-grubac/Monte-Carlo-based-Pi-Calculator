using System.Globalization;
using System.Text.RegularExpressions;

namespace MonteCarloPi.Desktop.ViewModels;

internal static partial class Format
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>For example "12,345,678".</summary>
    public static string Count(long value) => value.ToString("N0", Culture);

    /// <summary>For example "4.1 × 10⁵".</summary>
    public static string Scientific(double value)
    {
        if (value < 1000)
            return value.ToString("0", Culture);

        int exponent = (int)Math.Floor(Math.Log10(value));
        double mantissa = value / Math.Pow(10, exponent);
        return $"{mantissa.ToString("0.#", Culture)} × 10{Superscript(exponent)}";
    }

    /// <summary>For example "1K", "100M", "1B".</summary>
    public static string Short(long value) => value switch
    {
        >= 1_000_000_000 => (value / 1e9).ToString("0.#", Culture) + "B",
        >= 1_000_000 => (value / 1e6).ToString("0.#", Culture) + "M",
        >= 1_000 => (value / 1e3).ToString("0.#", Culture) + "K",
        _ => value.ToString(Culture),
    };

    public static string Signed(double value, int decimals = 6) =>
        double.IsNaN(value) ? "—" : (value >= 0 ? "+" : "−") + Math.Abs(value).ToString("F" + decimals, Culture);

    public static string Number(double value, int decimals) =>
        double.IsNaN(value) ? "—" : value.ToString("F" + decimals, Culture);

    public static string Rate(double perSecond) => perSecond switch
    {
        >= 1e9 => (perSecond / 1e9).ToString("0.0", Culture) + " B/s",
        >= 1e6 => (perSecond / 1e6).ToString("0.0", Culture) + " M/s",
        >= 1e3 => (perSecond / 1e3).ToString("0.0", Culture) + " K/s",
        _ => perSecond.ToString("0", Culture) + " /s",
    };

    public static string Duration(TimeSpan time) =>
        time.TotalMinutes >= 1 ? $"{(int)time.TotalMinutes}:{time.Seconds:00}" : time.TotalSeconds.ToString("0.0", Culture) + " s";

    /// <summary>
    /// Reads a point count written as "1000000", "1,000,000", "1 000 000", "1_000_000", "1e6", "2.5M" or "1B".
    /// </summary>
    public static long? ParseCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var match = CountPattern().Match(text.Trim().Replace(",", "").Replace("_", "").Replace(" ", ""));
        if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, Culture, out double value))
            return null;

        value *= match.Groups[2].Value.ToUpperInvariant() switch
        {
            "K" => 1e3,
            "M" => 1e6,
            "B" or "G" => 1e9,
            "T" => 1e12,
            _ => 1,
        };

        return value >= 1 && value <= long.MaxValue / 2 && value == Math.Floor(value) ? (long)value : null;
    }

    [GeneratedRegex(@"^(\d+(?:\.\d+)?(?:[eE]\d+)?)([kKmMbBgGtT]?)$")]
    private static partial Regex CountPattern();

    private static string Superscript(int number) =>
        string.Concat(number.ToString(Culture).Select(c => c switch
        {
            '-' => '⁻',
            '1' => '¹',
            '2' => '²',
            '3' => '³',
            _ => (char)('⁰' + (c - '0')),
        }));
}
