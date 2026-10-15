using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PerformanceMetricsSample;

/// <summary>
/// Controls the complete Syncfusion MAUI chart benchmark sequence.
/// </summary>
public sealed class BenchmarkViewModel : INotifyPropertyChanged
{
    private const int SeriesCount = 10;
    private const int PanStepCount = 30;
    private const int ZoomCycleCount = 5;
    private const int DelayAfterLoadMilliseconds = 1200;
    private const int RealTimeDurationMilliseconds = 2000;
    private const int RealTimeIntervalMilliseconds = 20;

    private readonly List<ObservableCollection<Model>> datasets = new();

    private IChartBenchmarkHost? chartHost;
    private CancellationTokenSource? cancellationSource;
    private Random random = new(7);

    private string selectedDataSize = "5K";
    private string datasetInformation = string.Empty;
    private string status = "Ready.";

    private string initialRenderResult = "-";
    private string memoryResult = "-";
    private string trackballResult = "-";
    private string panResult = "-";
    private string zoomResult = "-";
    private string uiResponseResult = "-";
    private string updatesPerSecondResult = "-";
    private string realTimeDurationResult = "-";

    private bool isBenchmarkRunning;
    private int currentXValue;
    private int updateCount;

    /// <summary>
    /// Initializes the dataset options and dashboard commands.
    /// </summary>
    public BenchmarkViewModel()
    {
        DataSizeOptions = new ObservableCollection<string>
        {
            "5K",
            "10K",
            "50K",
            "75K",
            "100K"
        };

        StartBenchmarkCommand = new AsyncCommand(
            RunBenchmarkAsync,
            () => chartHost != null && !IsBenchmarkRunning);

        ClearCommand = new RelayCommand(Clear);

        UpdateDatasetInformation();
    }

    /// <summary>
    /// Gets the available points-per-series values.
    /// </summary>
    public ObservableCollection<string> DataSizeOptions { get; }

    /// <summary>
    /// Gets the command that starts the benchmark.
    /// </summary>
    public ICommand StartBenchmarkCommand { get; }

    /// <summary>
    /// Gets the command that cancels and clears the benchmark.
    /// </summary>
    public ICommand ClearCommand { get; }

    /// <summary>
    /// Gets or sets the selected points-per-series value.
    /// </summary>
    public string SelectedDataSize
    {
        get => selectedDataSize;

        set
        {
            string newValue = string.IsNullOrWhiteSpace(value) ? "5K" : value;

            if (selectedDataSize == newValue)
            {
                return;
            }

            selectedDataSize = newValue;

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedDataCount));
            OnPropertyChanged(nameof(TotalDataCount));

            UpdateDatasetInformation();
        }
    }

    /// <summary>
    /// Gets the numeric point count for each individual series.
    /// </summary>
    public int SelectedDataCount =>
        SelectedDataSize switch
        {
            "5K" => 5_000,
            "10K" => 10_000,
            "50K" => 50_000,
            "75K" => 75_000,
            "100K" => 100_000,
            _ => 5_000
        };

    /// <summary>
    /// Gets the total point count across all ten series.
    /// </summary>
    public int TotalDataCount => SeriesCount * SelectedDataCount;

    public string DatasetInformation
    {
        get => datasetInformation;
        private set => SetProperty(ref datasetInformation, value);
    }

    public string Status
    {
        get => status;
        private set => SetProperty(ref status, value);
    }

    public bool IsBenchmarkRunning
    {
        get => isBenchmarkRunning;

        private set
        {
            if (!SetProperty(ref isBenchmarkRunning, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsBenchmarkReady));
            RaiseCommandStates();
        }
    }

    public bool IsBenchmarkReady => !IsBenchmarkRunning;

    public string InitialRenderResult
    {
        get => initialRenderResult;
        private set => SetProperty(ref initialRenderResult, value);
    }

    public string MemoryResult
    {
        get => memoryResult;
        private set => SetProperty(ref memoryResult, value);
    }

    public string TrackballResult
    {
        get => trackballResult;
        private set => SetProperty(ref trackballResult, value);
    }

    public string PanResult
    {
        get => panResult;
        private set => SetProperty(ref panResult, value);
    }

    public string ZoomResult
    {
        get => zoomResult;
        private set => SetProperty(ref zoomResult, value);
    }

    public string UiResponseResult
    {
        get => uiResponseResult;
        private set => SetProperty(ref uiResponseResult, value);
    }

    public string UpdatesPerSecondResult
    {
        get => updatesPerSecondResult;
        private set => SetProperty(ref updatesPerSecondResult, value);
    }

    public string RealTimeDurationResult
    {
        get => realTimeDurationResult;
        private set => SetProperty(ref realTimeDurationResult, value);
    }

    public int UpdateCount
    {
        get => updateCount;
        private set => SetProperty(ref updateCount, value);
    }

    /// <summary>
    /// Connects the ViewModel to the chart host.
    /// </summary>
    public void Initialize(IChartBenchmarkHost host)
    {
        chartHost = host ?? throw new ArgumentNullException(nameof(host));

        RaiseCommandStates();
    }

    /// <summary>
    /// Runs the complete chart benchmark.
    /// </summary>
    private async Task RunBenchmarkAsync()
    {
        if (chartHost == null || IsBenchmarkRunning)
        {
            return;
        }

        cancellationSource?.Cancel();
        cancellationSource?.Dispose();

        CancellationTokenSource currentSource = new();
        cancellationSource = currentSource;

        CancellationToken cancellationToken = currentSource.Token;

        try
        {
            IsBenchmarkRunning = true;

            ResetMetrics();

            Status = "Generating selected dataset...";

            GenerateDataset();

            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<IReadOnlyList<Model>> readOnlyDatasets =
                datasets
                    .Select(collection => (IReadOnlyList<Model>)collection)
                    .ToList();

            Status = "Measuring initial render...";

            InitialRenderMeasurement initialMeasurement =
                await chartHost.LoadAndRenderAsync(
                    readOnlyDatasets,
                    SeriesCount,
                    SelectedDataCount,
                    cancellationToken);

            InitialRenderResult =
                $"{initialMeasurement.ElapsedMilliseconds:F0} ms";

            MemoryResult =
                $"{initialMeasurement.MemoryDifferenceMegabytes:F1} MB";

            Status =
                $"Loaded {DatasetInformation}. Starting interaction tests...";

            await Task.Delay(
                DelayAfterLoadMilliseconds,
                cancellationToken);

            Status = "Measuring trackball response...";

            double trackballMilliseconds =
                await chartHost.MeasureTrackballAsync(
                    cancellationToken);

            Status = "Measuring pan / scroll performance...";

            double panMilliseconds =
                await RunPanBenchmarkAsync(
                    cancellationToken);

            Status = "Measuring zoom performance...";

            // The WPF sample clears its frame history before Zoom.
            // Therefore MAUI UI Response also represents Zoom timing.
            chartHost.BeginFrameCollection();

            double zoomMilliseconds =
                await RunZoomBenchmarkAsync(
                    cancellationToken);

            FrameMeasurement frameMeasurement =
                chartHost.EndFrameCollection();

            Status = "Measuring real-time update throughput...";

            RealTimeMeasurement realTimeMeasurement =
                await RunRealTimeBenchmarkAsync(
                    cancellationToken);

            TrackballResult =
                trackballMilliseconds > 0
                    ? $"{trackballMilliseconds:F2} ms"
                    : "Not available";

            PanResult =
                $"{panMilliseconds:F0} ms";

            ZoomResult =
                $"{zoomMilliseconds:F0} ms";

            UiResponseResult =
                frameMeasurement.FrameCount > 0
                    ? $"{frameMeasurement.AverageFrameMilliseconds:F2} ms/frame"
                    : "Not available";

            UpdatesPerSecondResult =
                $"{realTimeMeasurement.UpdatesPerSecond:F2}";

            RealTimeDurationResult =
                $"{realTimeMeasurement.DurationSeconds:F2} sec";

            Status = "Benchmark completed.";
        }
        catch (OperationCanceledException)
        {
            Status = "Benchmark cancelled.";
        }
        catch (Exception exception)
        {
            Status = $"Benchmark failed: {exception.Message}";
        }
        finally
        {
            if (ReferenceEquals(cancellationSource, currentSource))
            {
                cancellationSource = null;
            }

            currentSource.Dispose();

            IsBenchmarkRunning = false;
        }
    }

    /// <summary>
    /// Measures the total duration of thirty Pan operations.
    /// </summary>
    private async Task<double> RunPanBenchmarkAsync(
        CancellationToken cancellationToken)
    {
        if (chartHost == null)
        {
            return 0;
        }

        const double zoomFactor = 0.20;

        chartHost.ResetChart();

        await chartHost.WaitForRenderFrameAsync(
            cancellationToken);

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int index = 0; index < PanStepCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            double zoomPosition =
                index /
                (double)PanStepCount *
                (1.0 - zoomFactor);

            await chartHost.ApplyPanAsync(
                zoomFactor,
                zoomPosition,
                cancellationToken);

            // Wait for one presentation frame after each Pan operation.
            await chartHost.WaitForRenderFrameAsync(
                cancellationToken);
        }

        stopwatch.Stop();

        double totalPanMilliseconds =
            stopwatch.Elapsed.TotalMilliseconds;

        chartHost.ResetChart();

        await chartHost.WaitForRenderFrameAsync(
            cancellationToken);

        return totalPanMilliseconds;
    }

    /// <summary>
    /// Measures the total duration of fifteen Zoom operations.
    /// </summary>
    private async Task<double> RunZoomBenchmarkAsync(
        CancellationToken cancellationToken)
    {
        if (chartHost == null)
        {
            return 0;
        }

        chartHost.ResetChart();

        await chartHost.WaitForRenderFrameAsync(
            cancellationToken);

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int index = 0; index < ZoomCycleCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await RunZoomStepAsync(
                0.10,
                index * 0.15,
                cancellationToken);

            await RunZoomStepAsync(
                0.02,
                index * 0.15,
                cancellationToken);

            await RunZoomStepAsync(
                1.00,
                0,
                cancellationToken);
        }

        stopwatch.Stop();

        double totalZoomMilliseconds =
            stopwatch.Elapsed.TotalMilliseconds;

        chartHost.ResetChart();

        await chartHost.WaitForRenderFrameAsync(
            cancellationToken);

        return totalZoomMilliseconds;
    }

    /// <summary>
    /// Applies one Zoom operation and waits for one display frame.
    /// </summary>
    private async Task RunZoomStepAsync(
        double zoomFactor,
        double zoomPosition,
        CancellationToken cancellationToken)
    {
        if (chartHost == null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await chartHost.ApplyZoomAsync(
            zoomFactor,
            zoomPosition,
            cancellationToken);

        await chartHost.WaitForRenderFrameAsync(
            cancellationToken);
    }

    /// <summary>
    /// Generates ten deterministic datasets using the selected size.
    /// </summary>
    private void GenerateDataset()
    {
        ClearDataset();

        // Reset the seed so repeated runs use equivalent data.
        random = new Random(7);

        for (int seriesIndex = 0;
             seriesIndex < SeriesCount;
             seriesIndex++)
        {
            ObservableCollection<Model> collection = new();

            double yValue =
                random.NextDouble() * 10.0;

            for (int pointIndex = 0;
                 pointIndex < SelectedDataCount;
                 pointIndex++)
            {
                collection.Add(
                    new Model(
                        pointIndex,
                        yValue));

                yValue +=
                    random.NextDouble() > 0.5
                        ? random.NextDouble()
                        : -random.NextDouble();
            }

            datasets.Add(collection);
        }

        currentXValue = SelectedDataCount;

        UpdateDatasetInformation();
    }

    /// <summary>
    /// Runs the optional MAUI real-time update scenario.
    /// </summary>
    private async Task<RealTimeMeasurement>
        RunRealTimeBenchmarkAsync(
            CancellationToken cancellationToken)
    {
        UpdateCount = 0;

        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.ElapsedMilliseconds <
               RealTimeDurationMilliseconds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UpdateAllSeries();

            UpdateCount++;

            await Task.Delay(
                RealTimeIntervalMilliseconds,
                cancellationToken);
        }

        stopwatch.Stop();

        double durationSeconds =
            stopwatch.Elapsed.TotalSeconds;

        return new RealTimeMeasurement
        {
            DurationSeconds = durationSeconds,
            UpdatesPerSecond =
                durationSeconds > 0
                    ? UpdateCount / durationSeconds
                    : 0
        };
    }

    /// <summary>
    /// Performs one sliding-window update across all ten series.
    /// </summary>
    private void UpdateAllSeries()
    {
        foreach (ObservableCollection<Model> series in datasets)
        {
            if (series.Count == 0)
            {
                continue;
            }

            series.RemoveAt(0);

            double previousValue =
                series.Count > 0
                    ? series[^1].YValue
                    : 50;

            double change =
                random.NextDouble() > 0.5
                    ? random.NextDouble()
                    : -random.NextDouble();

            series.Add(
                new Model(
                    currentXValue,
                    previousValue + change));
        }

        currentXValue++;
    }

    /// <summary>
    /// Cancels and clears the current benchmark.
    /// </summary>
    private void Clear()
    {
        cancellationSource?.Cancel();

        chartHost?.ClearChart();

        ClearDataset();
        ResetMetrics();

        currentXValue = 0;
        UpdateCount = 0;

        Status = "Cleared. Ready.";
    }

    /// <summary>
    /// Clears all generated data collections.
    /// </summary>
    private void ClearDataset()
    {
        foreach (ObservableCollection<Model> series in datasets)
        {
            series.Clear();
        }

        datasets.Clear();
    }

    /// <summary>
    /// Resets all dashboard results.
    /// </summary>
    private void ResetMetrics()
    {
        InitialRenderResult = "-";
        MemoryResult = "-";
        TrackballResult = "-";
        PanResult = "-";
        ZoomResult = "-";
        UiResponseResult = "-";
        UpdatesPerSecondResult = "-";
        RealTimeDurationResult = "-";
    }

    /// <summary>
    /// Updates the selected dataset description.
    /// </summary>
    private void UpdateDatasetInformation()
    {
        DatasetInformation =
            $"{SeriesCount} series x " +
            $"{SelectedDataCount:N0} points = " +
            $"{TotalDataCount:N0} total points";
    }

    /// <summary>
    /// Requests the commands to reevaluate CanExecute.
    /// </summary>
    private void RaiseCommandStates()
    {
        if (StartBenchmarkCommand is AsyncCommand startCommand)
        {
            startCommand.RaiseCanExecuteChanged();
        }

        if (ClearCommand is RelayCommand clearCommand)
        {
            clearCommand.RaiseCanExecuteChanged();
        }
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;

        OnPropertyChanged(propertyName);

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }

    private sealed class RealTimeMeasurement
    {
        public double UpdatesPerSecond { get; init; }

        public double DurationSeconds { get; init; }
    }
}