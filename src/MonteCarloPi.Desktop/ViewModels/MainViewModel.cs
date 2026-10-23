using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonteCarloPi.Core;

namespace MonteCarloPi.Desktop.ViewModels;

public enum RunMode
{
    /// <summary>Draw a fixed number of points.</summary>
    Points,

    /// <summary>Draw until the estimate's first decimals equal π's.</summary>
    Decimals,
}

public enum RunState
{
    Idle,
    Running,
    Paused,
    Finished,
}

/// <summary>A point of the convergence chart: the estimate after <see cref="Total"/> points.</summary>
public readonly record struct ConvergencePoint(long Total, double Pi);

/// <summary>A quick choice for the number of points, for example "1M".</summary>
public sealed record PointPreset(string Label, long Points);

/// <summary>Points to draw, copied from one batch of the simulation.</summary>
public sealed record PointBatch(SamplePoint[] Points, int Count, long TotalBefore);

public sealed partial class MainViewModel : ObservableObject
{
    public const int MinDecimals = 1;
    public const int MaxDecimals = 8;

    /// <summary>The "until decimals match" mode gives up after this many points.</summary>
    public const long DecimalsBudget = 10_000_000_000;

    private const int CapturePerFrame = 4000;
    private const double FrameMs = 16;
    private const double BatchMs = 100;

    /// <summary>Points drawn slowly at the start, so the picture visibly fills in.</summary>
    private const long WarmUpPoints = 300_000;

    private readonly SamplePoint[] _capture = new SamplePoint[CapturePerFrame];
    private readonly Stopwatch _runTime = new();
    private readonly List<ConvergencePoint> _convergence = [];
    private PiSimulation? _simulation;
    private CancellationTokenSource? _cancellation;
    private double _batchSize;

    public MainViewModel()
    {
        PointsText = Format.Count(1_000_000);
        Refresh();
    }

    /// <summary>Raised on the UI thread with points to draw.</summary>
    public event EventHandler<PointBatch>? PointsDrawn;

    /// <summary>Raised when the drawing should be cleared.</summary>
    public event EventHandler? Cleared;

    // ---- Settings ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPointsMode), nameof(IsDecimalsMode), nameof(Target), nameof(ChartMaxPoints))]
    private RunMode _mode = RunMode.Points;

    public bool IsPointsMode
    {
        get => Mode == RunMode.Points;
        set { if (value) Mode = RunMode.Points; }
    }

    public bool IsDecimalsMode
    {
        get => Mode == RunMode.Decimals;
        set { if (value) Mode = RunMode.Decimals; }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PointsError), nameof(Target), nameof(ChartMaxPoints))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _pointsText;

    public long? Points => Format.ParseCount(PointsText);

    public string? PointsError => Points is null ? "Enter a whole number of points, for example 1000000, 1e6 or 2.5M." : null;

    public IReadOnlyList<PointPreset> PointPresets { get; } =
        new long[] { 10_000, 1_000_000, 100_000_000, 1_000_000_000 }.Select(n => new PointPreset(Format.Short(n), n)).ToArray();

    [RelayCommand]
    private void SetPoints(PointPreset preset) => PointsText = Format.Count(preset.Points);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExpectedPointsText), nameof(DecimalsWindowText), nameof(ChartMaxPoints))]
    [NotifyCanExecuteChangedFor(nameof(MoreDecimalsCommand), nameof(FewerDecimalsCommand))]
    private int _decimals = 3;

    [RelayCommand(CanExecute = nameof(CanAddDecimal))]
    private void MoreDecimals() => Decimals++;
    private bool CanAddDecimal() => Decimals < MaxDecimals && State is RunState.Idle or RunState.Finished;

    [RelayCommand(CanExecute = nameof(CanRemoveDecimal))]
    private void FewerDecimals() => Decimals--;
    private bool CanRemoveDecimal() => Decimals > MinDecimals && State is RunState.Idle or RunState.Finished;

    public string DecimalsWindowText
    {
        get
        {
            var (lower, upper) = PiMath.DecimalWindow(Decimals);
            return $"Stops at the first point where the estimate is in [{lower.ToString("F" + Decimals, CultureInfo.InvariantCulture)}, {upper.ToString("F" + Decimals, CultureInfo.InvariantCulture)}).";
        }
    }

    public string ExpectedPointsText =>
        $"To be 95% sure of {Decimals} {(Decimals == 1 ? "decimal" : "decimals")} you would need about {Format.Scientific(PiMath.SamplesForDecimals(Decimals))} points.";

    /// <summary>Blank for a random seed; a number repeats a run exactly.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SeedError))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _seedText = "";

    public string? SeedError => SeedText.Trim().Length > 0 && !ulong.TryParse(SeedText.Trim(), CultureInfo.InvariantCulture, out _)
        ? "The seed is a whole number from 0 to 18446744073709551615, or blank." : null;

    [ObservableProperty]
    private string? _usedSeed;

    /// <summary>The number of points the current run aims for.</summary>
    public long Target => Mode == RunMode.Points ? Points ?? 0 : DecimalsBudget;

    /// <summary>Where the chart's axis ends: the target, or the points a decimal search usually needs.</summary>
    public long ChartMaxPoints => Mode == RunMode.Points ? Points ?? 1 : (long)PiMath.SamplesForDecimals(Decimals);

    // ---- State ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning), nameof(CanEditSettings), nameof(StartLabel))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(PauseCommand), nameof(ResetCommand), nameof(MoreDecimalsCommand), nameof(FewerDecimalsCommand))]
    private RunState _state;

    public bool IsRunning => State == RunState.Running;

    public bool CanEditSettings => State is RunState.Idle or RunState.Finished;

    public string StartLabel => State switch
    {
        RunState.Paused => "Resume",
        RunState.Finished => "Run again",
        _ => "Start",
    };

    [ObservableProperty]
    private string _status = "Press Start to throw the first points.";

    [ObservableProperty]
    private double _progress;

    // ---- Results ----

    [ObservableProperty]
    private PiEstimate _estimate;

    /// <summary>The digits of the estimate that equal π's.</summary>
    [ObservableProperty]
    private string _correctDigits = "";

    /// <summary>The digits after the first wrong one.</summary>
    [ObservableProperty]
    private string _otherDigits = "?.??????";

    [ObservableProperty]
    private string _errorText = "—";

    [ObservableProperty]
    private string _intervalText = "—";

    [ObservableProperty]
    private string _countText = "0";

    [ObservableProperty]
    private string _insideText = "0";

    [ObservableProperty]
    private string _matchingText = "0";

    [ObservableProperty]
    private string _speedText = "—";

    [ObservableProperty]
    private string _elapsedText = "0.0 s";

    [ObservableProperty]
    private IReadOnlyList<ConvergencePoint> _history = [];

    public string PiDigits => PiMath.Digits;

    // ---- Commands ----

    private bool CanStart() => State != RunState.Running && SeedError is null && (Mode == RunMode.Decimals || Points is not null);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (State is RunState.Idle or RunState.Finished)
            BeginRun();

        State = RunState.Running;
        _cancellation = new CancellationTokenSource();
        _runTime.Start();
        try
        {
            await RunLoopAsync(_cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // Paused or reset.
        }
        finally
        {
            _runTime.Stop();
            Refresh();
        }
    }

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Pause()
    {
        _cancellation?.Cancel();
        State = RunState.Paused;
        Status = "Paused.";
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        _cancellation?.Cancel();
        _simulation = null;
        _convergence.Clear();
        _runTime.Reset();
        UsedSeed = null;
        Progress = 0;
        State = RunState.Idle;
        Status = "Press Start to throw the first points.";
        Cleared?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    private bool CanReset() => State != RunState.Idle;

    private void BeginRun()
    {
        ulong seed = ulong.TryParse(SeedText.Trim(), CultureInfo.InvariantCulture, out ulong s) ? s : (ulong)Random.Shared.NextInt64();
        _simulation = new PiSimulation(seed);
        UsedSeed = seed.ToString(CultureInfo.InvariantCulture);
        _convergence.Clear();
        _runTime.Reset();
        _batchSize = 25;
        Progress = 0;
        Cleared?.Invoke(this, EventArgs.Empty);
        Refresh();
    }

    /// <summary>
    /// Runs the simulation batch by batch. Batches start tiny, so the first points can be watched as they land,
    /// and after <see cref="WarmUpPoints"/> each one is sized to take about <see cref="BatchMs"/>.
    /// </summary>
    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        var simulation = _simulation!;
        var mode = Mode;
        int decimals = Decimals;
        long target = Target;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long remaining = target - simulation.Estimate.Total;
            if (remaining <= 0)
            {
                Finish(mode == RunMode.Points
                    ? $"Done: {Format.Count(simulation.Estimate.Total)} points."
                    : $"No match within {Format.Short(DecimalsBudget)} points. Monte Carlo converges slowly.");
                return;
            }

            long batch = Math.Min(remaining, (long)_batchSize);
            long before = simulation.Estimate.Total;
            var watch = Stopwatch.StartNew();

            var (captured, matched) = await Task.Run(() => mode == RunMode.Points
                ? (simulation.Run(batch, _capture, cancellationToken), false)
                : Swap(simulation.RunUntilDecimalsMatch(decimals, batch, _capture)), cancellationToken);

            double ms = watch.Elapsed.TotalMilliseconds;
            PointsDrawn?.Invoke(this, new PointBatch(_capture, captured, before));
            Record(simulation.Estimate);
            Refresh();

            if (matched)
            {
                Finish($"{Decimals} {(Decimals == 1 ? "decimal matches" : "decimals match")} after {Format.Count(simulation.Estimate.Total)} points.");
                return;
            }

            // While the first points land, grow slowly so they can be watched; then size each batch
            // from the measured speed so it takes about BatchMs and the cores stay busy.
            double perMs = batch / Math.Max(ms, 0.25);
            if (simulation.Estimate.Total < WarmUpPoints)
            {
                _batchSize = Math.Min(_batchSize * 1.12, perMs * FrameMs);
                if (ms < FrameMs)
                    await Task.Delay(TimeSpan.FromMilliseconds(FrameMs - ms), cancellationToken);
            }
            else
            {
                _batchSize = Math.Clamp(perMs * BatchMs, _batchSize / 2, _batchSize * 8);
            }
        }

        static (int, bool) Swap((bool Matched, int Captured) result) => (result.Captured, result.Matched);
    }

    private void Finish(string message)
    {
        State = RunState.Finished;
        Status = message;
        Progress = Mode == RunMode.Points ? 1 : Progress;
    }

    // Log-spaced history: a new point whenever the count grows by 3%.
    private void Record(PiEstimate estimate)
    {
        if (estimate.Total == 0)
            return;
        if (_convergence.Count == 0 || estimate.Total >= _convergence[^1].Total * 1.03)
            _convergence.Add(new ConvergencePoint(estimate.Total, estimate.Pi));
        else
            _convergence[^1] = new ConvergencePoint(estimate.Total, estimate.Pi);
        History = _convergence.ToArray();
    }

    private void Refresh()
    {
        var estimate = _simulation?.Estimate ?? default;
        Estimate = estimate;

        if (estimate.IsEmpty)
        {
            CorrectDigits = "";
            OtherDigits = "?.??????";
            ErrorText = IntervalText = SpeedText = "—";
            CountText = InsideText = MatchingText = "0";
            ElapsedText = "0.0 s";
            History = [];
            return;
        }

        // 15 decimals is the most a double can say; show 10 in the headline.
        string digits = estimate.Pi.ToString("F10", CultureInfo.InvariantCulture);
        int correct = Math.Min(estimate.MatchingDecimals, 10);
        int split = estimate.Pi is >= 3 and < 4 ? 2 + correct : 0;
        CorrectDigits = digits[..split];
        OtherDigits = digits[split..];

        ErrorText = Format.Signed(estimate.Error);
        IntervalText = $"± {Format.Number(PiEstimate.Z95 * estimate.StandardError, 6)}";
        CountText = Format.Count(estimate.Total);
        InsideText = Format.Count(estimate.Inside);
        MatchingText = estimate.MatchingDecimals.ToString(CultureInfo.InvariantCulture);
        ElapsedText = Format.Duration(_runTime.Elapsed);
        SpeedText = _runTime.Elapsed.TotalSeconds > 0.2 ? Format.Rate(estimate.Total / _runTime.Elapsed.TotalSeconds) : "—";

        if (Mode == RunMode.Points && Target > 0)
            Progress = (double)estimate.Total / Target;
        else
            Progress = Math.Min(1, Math.Log10(estimate.Total) / Math.Log10(PiMath.SamplesForDecimals(Decimals)));

        if (State == RunState.Running)
            Status = Mode == RunMode.Points
                ? $"Throwing points… {Format.Short(estimate.Total)} of {Format.Short(Target)}"
                : $"Looking for {Decimals} matching {(Decimals == 1 ? "decimal" : "decimals")}… {Format.Short(estimate.Total)} points so far";
    }

    partial void OnModeChanged(RunMode value) => StartCommand.NotifyCanExecuteChanged();
}
