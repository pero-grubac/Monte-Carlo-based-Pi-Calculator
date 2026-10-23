using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MonteCarloPi.Core;

namespace MonteCarloPi.Desktop.Controls;

/// <summary>
/// The square [−1, 1]² with the unit circle, where every drawn point stays painted.
/// Points are written into a bitmap, so millions of them cost nothing to show.
/// </summary>
public sealed class PointCloudView : FrameworkElement
{
    private const int Resolution = 800;

    /// <summary>Points drawn as 3×3 dots until there are this many; single pixels after that.</summary>
    private const long LargeDotsUntil = 20_000;

    public static readonly DependencyProperty InsideBrushProperty = Register(nameof(InsideBrush));
    public static readonly DependencyProperty OutsideBrushProperty = Register(nameof(OutsideBrush));
    public static readonly DependencyProperty OutlineBrushProperty = Register(nameof(OutlineBrush));

    private readonly WriteableBitmap _bitmap = new(Resolution, Resolution, 96, 96, PixelFormats.Bgra32, null);
    private readonly int[] _pixels = new int[Resolution * Resolution];

    public Brush? InsideBrush
    {
        get => (Brush?)GetValue(InsideBrushProperty);
        set => SetValue(InsideBrushProperty, value);
    }

    public Brush? OutsideBrush
    {
        get => (Brush?)GetValue(OutsideBrushProperty);
        set => SetValue(OutsideBrushProperty, value);
    }

    public Brush? OutlineBrush
    {
        get => (Brush?)GetValue(OutlineBrushProperty);
        set => SetValue(OutlineBrushProperty, value);
    }

    private static DependencyProperty Register(string name) =>
        DependencyProperty.Register(name, typeof(Brush), typeof(PointCloudView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public void Clear()
    {
        Array.Clear(_pixels);
        _bitmap.WritePixels(new Int32Rect(0, 0, Resolution, Resolution), _pixels, Resolution * 4, 0);
    }

    public void Draw(ReadOnlySpan<SamplePoint> points, long totalBefore)
    {
        if (points.IsEmpty)
            return;

        int inside = Argb(InsideBrush, 0xFF2F80ED);
        int outside = Argb(OutsideBrush, 0xFFF2994A);
        int radius = totalBefore < LargeDotsUntil ? 1 : 0;

        foreach (var point in points)
        {
            int cx = (int)((point.X + 1) / 2 * (Resolution - 1));
            int cy = (int)((1 - point.Y) / 2 * (Resolution - 1));
            int color = point.Inside ? inside : outside;

            for (int y = Math.Max(0, cy - radius); y <= Math.Min(Resolution - 1, cy + radius); y++)
                for (int x = Math.Max(0, cx - radius); x <= Math.Min(Resolution - 1, cx + radius); x++)
                    _pixels[y * Resolution + x] = color;
        }

        _bitmap.WritePixels(new Int32Rect(0, 0, Resolution, Resolution), _pixels, Resolution * 4, 0);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        double side = Math.Min(availableSize.Width, availableSize.Height);
        return double.IsInfinity(side) ? new Size(400, 400) : new Size(side, side);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double side = Math.Min(ActualWidth, ActualHeight);
        var square = new Rect((ActualWidth - side) / 2, (ActualHeight - side) / 2, side, side);

        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        dc.DrawImage(_bitmap, square);

        var outline = new Pen(OutlineBrush, 1.5);
        dc.DrawRectangle(null, outline, square);
        dc.DrawEllipse(null, outline, new Point(square.X + side / 2, square.Y + side / 2), side / 2, side / 2);
    }

    private static int Argb(Brush? brush, uint fallback) =>
        brush is SolidColorBrush { Color: var c } ? (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B : unchecked((int)fallback);
}
