using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MonteCarloPi.Core;
using MonteCarloPi.Desktop.ViewModels;

namespace MonteCarloPi.Desktop.Controls;

/// <summary>
/// The estimate minus π against the number of points (log scale), inside the theoretical
/// 95% band ± 1.96 · σ₁ / √N that every run should stay in most of the time.
/// </summary>
public sealed class ConvergenceChart : FrameworkElement
{
    private const double Left = 64;
    private const double Bottom = 22;
    private const double Top = 8;

    public static readonly DependencyProperty HistoryProperty = Register<IReadOnlyList<ConvergencePoint>?>(nameof(History), null);
    public static readonly DependencyProperty MaxPointsProperty = Register(nameof(MaxPoints), 1_000_000L);
    public static readonly DependencyProperty LineBrushProperty = Register<Brush?>(nameof(LineBrush), Brushes.SteelBlue);
    public static readonly DependencyProperty BandBrushProperty = Register<Brush?>(nameof(BandBrush), Brushes.LightGray);
    public static readonly DependencyProperty ForegroundProperty = Register<Brush?>(nameof(Foreground), Brushes.Gray);
    public static readonly DependencyProperty AxisBrushProperty = Register<Brush?>(nameof(AxisBrush), Brushes.DimGray);

    public IReadOnlyList<ConvergencePoint>? History
    {
        get => (IReadOnlyList<ConvergencePoint>?)GetValue(HistoryProperty);
        set => SetValue(HistoryProperty, value);
    }

    /// <summary>Where the x axis ends, at least.</summary>
    public long MaxPoints
    {
        get => (long)GetValue(MaxPointsProperty);
        set => SetValue(MaxPointsProperty, value);
    }

    public Brush? LineBrush
    {
        get => (Brush?)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public Brush? BandBrush
    {
        get => (Brush?)GetValue(BandBrushProperty);
        set => SetValue(BandBrushProperty, value);
    }

    public Brush? Foreground
    {
        get => (Brush?)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public Brush? AxisBrush
    {
        get => (Brush?)GetValue(AxisBrushProperty);
        set => SetValue(AxisBrushProperty, value);
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(name, typeof(T), typeof(ConvergenceChart),
            new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));

    protected override void OnRender(DrawingContext dc)
    {
        double width = ActualWidth - Left - 8;
        double height = ActualHeight - Top - Bottom;
        if (width < 40 || height < 40)
            return;

        var history = History ?? [];
        long lastTotal = history.Count > 0 ? history[^1].Total : 1;
        double maxLog = Math.Max(2, Math.Ceiling(Math.Log10(Math.Max(Math.Max(MaxPoints, lastTotal), 10))));

        // The y range follows the run: wide at first, zooming in as the estimate settles.
        double range = 0.02;
        foreach (var point in history.Where(p => p.Total >= 100))
            range = Math.Max(range, Math.Abs(point.Pi - Math.PI));
        if (history.Count > 0)
        {
            double recent = history.Where(p => p.Total >= lastTotal / 10).Max(p => Math.Abs(p.Pi - Math.PI));
            double band = Band(lastTotal);
            range = Math.Min(range, Math.Max(Math.Max(recent, band) * 2.2, 1e-5));
        }

        double X(double total) => Left + Math.Log10(Math.Max(1, total)) / maxLog * width;
        double Y(double error) => Top + height / 2 - Math.Clamp(error / range, -1.05, 1.05) * height / 2;

        // 95% band of the theory.
        var bandGeometry = new StreamGeometry();
        using (var g = bandGeometry.Open())
        {
            const int steps = 120;
            var upper = Enumerable.Range(0, steps + 1).Select(i => Math.Pow(10, 1 + (maxLog - 1) * i / steps)).ToArray();
            g.BeginFigure(new Point(X(upper[0]), Y(Band(upper[0]))), true, true);
            g.PolyLineTo(upper.Skip(1).Select(n => new Point(X(n), Y(Band(n)))).ToArray(), false, false);
            g.PolyLineTo(upper.Reverse().Select(n => new Point(X(n), Y(-Band(n)))).ToArray(), false, false);
        }
        dc.PushClip(new RectangleGeometry(new Rect(Left, Top, width, height)));
        dc.DrawGeometry(BandBrush, null, bandGeometry);

        // π itself.
        dc.DrawLine(new Pen(Foreground, 1) { DashStyle = DashStyles.Dash }, new Point(Left, Y(0)), new Point(Left + width, Y(0)));

        // The run.
        if (history.Count > 1)
        {
            var line = new StreamGeometry();
            using (var g = line.Open())
            {
                g.BeginFigure(new Point(X(history[0].Total), Y(history[0].Pi - Math.PI)), false, false);
                g.PolyLineTo(history.Skip(1).Select(p => new Point(X(p.Total), Y(p.Pi - Math.PI))).ToArray(), true, true);
            }
            dc.DrawGeometry(null, new Pen(LineBrush, 2) { LineJoin = PenLineJoin.Round }, line);
        }
        if (history.Count > 0)
            dc.DrawEllipse(LineBrush, null, new Point(X(lastTotal), Y(history[^1].Pi - Math.PI)), 4, 4);
        dc.Pop();

        // Axes and labels.
        var axis = new Pen(AxisBrush, 1);
        dc.DrawLine(axis, new Point(Left, Top + height), new Point(Left + width, Top + height));
        for (int p = 1; p <= maxLog; p++)
        {
            double x = X(Math.Pow(10, p));
            dc.DrawLine(axis, new Point(x, Top + height), new Point(x, Top + height + 4));
            if (p % (maxLog > 8 ? 2 : 1) == 0 || p == maxLog)
            {
                var label = Text(Format.Short((long)Math.Pow(10, p)));
                dc.DrawText(label, new Point(Math.Min(x - label.Width / 2, ActualWidth - label.Width), Top + height + 5));
            }
        }

        string Label(double error) => (error > 0 ? "π + " : "π − ") + Math.Abs(error).ToString(range < 0.001 ? "0.0e0" : "0.####", CultureInfo.InvariantCulture);
        var top = Text(Label(range));
        var bottom = Text(Label(-range));
        var mid = Text("π");
        dc.DrawText(top, new Point(Left - 8 - top.Width, Top));
        dc.DrawText(mid, new Point(Left - 8 - mid.Width, Y(0) - mid.Height / 2));
        dc.DrawText(bottom, new Point(Left - 8 - bottom.Width, Top + height - bottom.Height));
    }

    private static double Band(double total) => PiEstimate.Z95 * PiMath.SingleSampleDeviation / Math.Sqrt(total);

    private FormattedText Text(string text) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Foreground,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
