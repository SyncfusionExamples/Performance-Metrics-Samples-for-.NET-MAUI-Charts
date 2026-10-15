using System.Diagnostics;

#if ANDROID
using Android.Views;
#endif

#if WINDOWS
using Microsoft.UI.Xaml.Media;
#endif

namespace PerformanceMetricsSample;

/// <summary>
/// Performs the chart-specific operations requested by BenchmarkViewModel.
/// </summary>
public partial class MainPage : ContentPage, IChartBenchmarkHost
{
    private const int RequiredSeriesCount = 10;
    private const int RenderTimeoutMilliseconds = 30000;
    private const int TrackballTimeoutMilliseconds = 5000;

    private readonly HashSet<FastLineSeriesExt> renderedSeries = new();
    private readonly List<double> frameTimes = new();
    private readonly object frameLock = new();

    private TaskCompletionSource<bool>? initialRenderCompletionSource;
    private TaskCompletionSource<bool>? trackballCompletionSource;

    private int expectedSeriesCount;
    private int expectedPointCountPerSeries;
    private bool isFrameCollectionActive;

#if ANDROID
    private AndroidContinuousFrameCallback? androidFrameMonitor;
#endif

#if WINDOWS
    private EventHandler<object>? windowsRenderingHandler;
    private TimeSpan? previousWindowsRenderingTime;
#endif

    public BenchmarkViewModel ViewModel { get; }

    /// <summary>
    /// Initializes the ViewModel and subscribes to chart events.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();

        ViewModel = new BenchmarkViewModel();
        BindingContext = ViewModel;

        ViewModel.Initialize(this);

        benchmarkSeries1.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries2.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries3.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries4.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries5.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries6.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries7.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries8.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries9.RenderCompleted += OnSeriesRenderCompleted;
        benchmarkSeries10.RenderCompleted += OnSeriesRenderCompleted;

        chart.TrackballCreated += OnTrackballCreated;
    }

    /// <summary>
    /// Measures initial rendering and managed-memory growth.
    /// </summary>
    public async Task<InitialRenderMeasurement> LoadAndRenderAsync(
        IReadOnlyList<IReadOnlyList<Model>> datasets,
        int expectedSeriesCount,
        int expectedPointCountPerSeries,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDatasets(
            datasets,
            expectedSeriesCount,
            expectedPointCountPerSeries);

        // ClearChart resets expected-count fields, so call it first.
        ClearChart();

        this.expectedSeriesCount = expectedSeriesCount;
        this.expectedPointCountPerSeries =
            expectedPointCountPerSeries;

        // Allow the previous clear operation to reach a display frame.
        await WaitForRenderFrameAsync(
            cancellationToken);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryBefore =
            GC.GetTotalMemory(true);

        renderedSeries.Clear();

        initialRenderCompletionSource =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Stopwatch stopwatch = Stopwatch.StartNew();

        AssignSeriesItemsSources(datasets);

        bool rendered =
            await WaitForInitialRenderCompletionAsync(
                cancellationToken);

        if (!rendered)
        {
            stopwatch.Stop();

            throw new TimeoutException(
                "All chart series did not render within the timeout.");
        }

        // Allow DrawSeries output to reach the next presentation frame.
        await WaitForRenderFrameAsync(
            cancellationToken);

        stopwatch.Stop();

        long memoryAfter =
            GC.GetTotalMemory(true);

        return new InitialRenderMeasurement
        {
            ElapsedMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds,

            MemoryBeforeBytes =
                memoryBefore,

            MemoryAfterBytes =
                memoryAfter
        };
    }

    /// <summary>
    /// Measures programmatic Trackball response.
    /// </summary>
    public async Task<double> MeasureTrackballAsync(
        CancellationToken cancellationToken)
    {
        if (benchmarkSeries1.ItemsSource is not
            IEnumerable<Model> source)
        {
            return 0;
        }

        List<Model> points =
            source.ToList();

        if (points.Count == 0)
        {
            return 0;
        }

        trackballBehavior.Hide();

        await WaitForRenderFrameAsync(
            cancellationToken);

        int middleIndex =
            points.Count / 2;

        float xPosition =
            chart.ValueToPoint(
                xAxis,
                points[middleIndex].XValue);

        float yPosition =
            (float)Math.Max(
                1,
                chart.Height / 2);

        if (float.IsNaN(xPosition) ||
            float.IsInfinity(xPosition))
        {
            return 0;
        }

        trackballCompletionSource =
            new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        Stopwatch stopwatch = Stopwatch.StartNew();

        trackballBehavior.Show(
            xPosition,
            yPosition);

        bool created =
            await WaitForTrackballCompletionAsync(
                cancellationToken);

        if (created)
        {
            await WaitForRenderFrameAsync(
                cancellationToken);
        }

        stopwatch.Stop();

        trackballBehavior.Hide();

        return created
            ? stopwatch.Elapsed.TotalMilliseconds
            : 0;
    }

    /// <summary>
    /// Waits for the next platform display frame.
    /// </summary>
    public Task WaitForRenderFrameAsync(
        CancellationToken cancellationToken)
    {
        return WaitForNextRenderFrameAsync(
            cancellationToken);
    }

    /// <summary>
    /// Applies one horizontal Pan operation.
    /// </summary>
    public Task ApplyPanAsync(
        double zoomFactor,
        double zoomPosition,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        double factor =
            Math.Clamp(
                zoomFactor,
                0.001,
                1.0);

        xAxis.ZoomFactor = factor;

        xAxis.ZoomPosition =
            Math.Clamp(
                zoomPosition,
                0,
                1.0 - factor);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies one Zoom operation to both axes.
    /// </summary>
    public Task ApplyZoomAsync(
        double zoomFactor,
        double zoomPosition,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        double factor =
            Math.Clamp(
                zoomFactor,
                0.001,
                1.0);

        double maximumPosition =
            1.0 - factor;

        xAxis.ZoomFactor = factor;

        xAxis.ZoomPosition =
            Math.Clamp(
                zoomPosition,
                0,
                maximumPosition);

        yAxis.ZoomFactor = factor;
        yAxis.ZoomPosition = 0;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts continuous frame-interval collection.
    /// </summary>
    public void BeginFrameCollection()
    {
        StopFrameCollection();

        lock (frameLock)
        {
            frameTimes.Clear();
        }

        isFrameCollectionActive = true;

#if ANDROID
        androidFrameMonitor =
            new AndroidContinuousFrameCallback(
                this);

        Choreographer.Instance.PostFrameCallback(
            androidFrameMonitor);
#elif WINDOWS
        previousWindowsRenderingTime = null;
        windowsRenderingHandler = OnWindowsRendering;

        CompositionTarget.Rendering +=
            windowsRenderingHandler;
#endif
    }

    /// <summary>
    /// Stops frame monitoring and calculates average UI response.
    /// </summary>
    public FrameMeasurement EndFrameCollection()
    {
        StopFrameCollection();

        lock (frameLock)
        {
            if (frameTimes.Count == 0)
            {
                return new FrameMeasurement();
            }

            return new FrameMeasurement
            {
                FrameCount = frameTimes.Count,
                TotalDurationMilliseconds = frameTimes.Sum(),
                AverageFrameMilliseconds = frameTimes.Average()
            };
        }
    }

    /// <summary>
    /// Restores the unzoomed chart state.
    /// </summary>
    public void ResetChart()
    {
        xAxis.ZoomFactor = 1;
        xAxis.ZoomPosition = 0;

        yAxis.ZoomFactor = 1;
        yAxis.ZoomPosition = 0;

        trackballBehavior.Hide();
    }

    /// <summary>
    /// Clears all chart data and active measurement state.
    /// </summary>
    public void ClearChart()
    {
        StopFrameCollection();
        CancelPendingOperations();

        benchmarkSeries1.ItemsSource = null;
        benchmarkSeries2.ItemsSource = null;
        benchmarkSeries3.ItemsSource = null;
        benchmarkSeries4.ItemsSource = null;
        benchmarkSeries5.ItemsSource = null;
        benchmarkSeries6.ItemsSource = null;
        benchmarkSeries7.ItemsSource = null;
        benchmarkSeries8.ItemsSource = null;
        benchmarkSeries9.ItemsSource = null;
        benchmarkSeries10.ItemsSource = null;

        renderedSeries.Clear();

        lock (frameLock)
        {
            frameTimes.Clear();
        }

        expectedSeriesCount = 0;
        expectedPointCountPerSeries = 0;

        ResetChart();
    }

    private static void ValidateDatasets(
        IReadOnlyList<IReadOnlyList<Model>> datasets,
        int expectedSeriesCount,
        int expectedPointCountPerSeries)
    {
        if (expectedSeriesCount != RequiredSeriesCount)
        {
            throw new InvalidOperationException(
                $"This page requires {RequiredSeriesCount} datasets.");
        }

        if (datasets.Count != expectedSeriesCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedSeriesCount} datasets, " +
                $"but received {datasets.Count}.");
        }

        for (int index = 0;
             index < datasets.Count;
             index++)
        {
            if (datasets[index].Count !=
                expectedPointCountPerSeries)
            {
                throw new InvalidOperationException(
                    $"Series {index + 1} contains " +
                    $"{datasets[index].Count:N0} points. " +
                    $"Expected {expectedPointCountPerSeries:N0} points.");
            }
        }
    }

    private void AssignSeriesItemsSources(
        IReadOnlyList<IReadOnlyList<Model>> datasets)
    {
        benchmarkSeries1.ItemsSource = datasets[0];
        benchmarkSeries2.ItemsSource = datasets[1];
        benchmarkSeries3.ItemsSource = datasets[2];
        benchmarkSeries4.ItemsSource = datasets[3];
        benchmarkSeries5.ItemsSource = datasets[4];
        benchmarkSeries6.ItemsSource = datasets[5];
        benchmarkSeries7.ItemsSource = datasets[6];
        benchmarkSeries8.ItemsSource = datasets[7];
        benchmarkSeries9.ItemsSource = datasets[8];
        benchmarkSeries10.ItemsSource = datasets[9];
    }

    private async Task<bool>
        WaitForInitialRenderCompletionAsync(
            CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool>? source =
            initialRenderCompletionSource;

        if (source == null)
        {
            return false;
        }

        Task timeout =
            Task.Delay(
                RenderTimeoutMilliseconds,
                cancellationToken);

        Task completed =
            await Task.WhenAny(
                source.Task,
                timeout);

        cancellationToken.ThrowIfCancellationRequested();

        bool rendered =
            completed == source.Task;

        initialRenderCompletionSource = null;

        return rendered;
    }

    private async Task<bool>
        WaitForTrackballCompletionAsync(
            CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool>? source =
            trackballCompletionSource;

        if (source == null)
        {
            return false;
        }

        Task timeout =
            Task.Delay(
                TrackballTimeoutMilliseconds,
                cancellationToken);

        Task completed =
            await Task.WhenAny(
                source.Task,
                timeout);

        cancellationToken.ThrowIfCancellationRequested();

        bool created =
            completed == source.Task;

        trackballCompletionSource = null;

        return created;
    }

    /// <summary>
    /// Waits for one platform rendering callback.
    /// </summary>
    private Task WaitForNextRenderFrameAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if ANDROID
        TaskCompletionSource<bool> source =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationTokenRegistration registration =
            cancellationToken.Register(
                () =>
                    source.TrySetCanceled(
                        cancellationToken));

        AndroidSingleFrameCallback callback =
            new(
                source,
                registration);

        Choreographer.Instance.PostFrameCallback(
            callback);

        return source.Task;
#elif WINDOWS
        TaskCompletionSource<bool> source =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        EventHandler<object>? handler = null;
        CancellationTokenRegistration registration = default;

        handler =
            (sender, arguments) =>
            {
                CompositionTarget.Rendering -= handler;

                registration.Dispose();

                source.TrySetResult(true);
            };

        registration =
            cancellationToken.Register(
                () =>
                {
                    CompositionTarget.Rendering -= handler;

                    source.TrySetCanceled(
                        cancellationToken);
                });

        CompositionTarget.Rendering += handler;

        return source.Task;
#else
        TaskCompletionSource<bool> source =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationTokenRegistration registration =
            cancellationToken.Register(
                () =>
                    source.TrySetCanceled(
                        cancellationToken));

        Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(16),
            () =>
            {
                registration.Dispose();
                source.TrySetResult(true);
            });

        return source.Task;
#endif
    }

    /// <summary>
    /// Stops continuous frame monitoring.
    /// </summary>
    private void StopFrameCollection()
    {
        isFrameCollectionActive = false;

#if ANDROID
        if (androidFrameMonitor != null)
        {
            Choreographer.Instance.RemoveFrameCallback(
                androidFrameMonitor);

            androidFrameMonitor = null;
        }
#elif WINDOWS
        if (windowsRenderingHandler != null)
        {
            CompositionTarget.Rendering -=
                windowsRenderingHandler;

            windowsRenderingHandler = null;
        }

        previousWindowsRenderingTime = null;
#endif
    }

#if WINDOWS
    /// <summary>
    /// Records intervals between consecutive Windows rendering events.
    /// </summary>
    private void OnWindowsRendering(
        object? sender,
        object eventArguments)
    {
        if (!isFrameCollectionActive)
        {
            return;
        }

        if (eventArguments is not
            RenderingEventArgs renderingArguments)
        {
            return;
        }

        TimeSpan currentTime =
            renderingArguments.RenderingTime;

        if (previousWindowsRenderingTime.HasValue)
        {
            double milliseconds =
                (currentTime -
                 previousWindowsRenderingTime.Value)
                .TotalMilliseconds;

            AddFrameTime(milliseconds);
        }

        previousWindowsRenderingTime =
            currentTime;
    }
#endif

    /// <summary>
    /// Adds one valid frame interval to the collection.
    /// </summary>
    private void AddFrameTime(
        double milliseconds)
    {
        if (milliseconds <= 0 ||
            milliseconds >= 1000)
        {
            return;
        }

        lock (frameLock)
        {
            frameTimes.Add(milliseconds);

            if (frameTimes.Count > 1000)
            {
                frameTimes.RemoveAt(0);
            }
        }
    }

    private void OnSeriesRenderCompleted(
        object? sender,
        EventArgs e)
    {
        if (initialRenderCompletionSource == null ||
            sender is not FastLineSeriesExt series ||
            series.ItemsSource is not ICollection<Model> items ||
            items.Count != expectedPointCountPerSeries)
        {
            return;
        }

        renderedSeries.Add(series);

        if (renderedSeries.Count >=
            expectedSeriesCount)
        {
            initialRenderCompletionSource.TrySetResult(
                true);
        }
    }

    private void OnTrackballCreated(
        object? sender,
        EventArgs e)
    {
        trackballCompletionSource?.TrySetResult(
            true);
    }

    private void CancelPendingOperations()
    {
        initialRenderCompletionSource?.TrySetCanceled();
        trackballCompletionSource?.TrySetCanceled();

        initialRenderCompletionSource = null;
        trackballCompletionSource = null;

        renderedSeries.Clear();
    }

    protected override void OnDisappearing()
    {
        StopFrameCollection();
        CancelPendingOperations();

        trackballBehavior.Hide();

        base.OnDisappearing();
    }

#if ANDROID
    /// <summary>
    /// Completes one awaited Android frame.
    /// </summary>
    private sealed class AndroidSingleFrameCallback :
        Java.Lang.Object,
        Choreographer.IFrameCallback
    {
        private readonly TaskCompletionSource<bool> source;
        private readonly CancellationTokenRegistration registration;

        public AndroidSingleFrameCallback(
            TaskCompletionSource<bool> source,
            CancellationTokenRegistration registration)
        {
            this.source = source;
            this.registration = registration;
        }

        public void DoFrame(
            long frameTimeNanos)
        {
            registration.Dispose();

            source.TrySetResult(true);
        }
    }

    /// <summary>
    /// Continuously records Android frame intervals during Zoom.
    /// </summary>
    private sealed class AndroidContinuousFrameCallback :
        Java.Lang.Object,
        Choreographer.IFrameCallback
    {
        private readonly WeakReference<MainPage> pageReference;
        private long? previousFrameTimeNanos;

        public AndroidContinuousFrameCallback(
            MainPage page)
        {
            pageReference =
                new WeakReference<MainPage>(
                    page);
        }

        public void DoFrame(
            long frameTimeNanos)
        {
            if (!pageReference.TryGetTarget(
                    out MainPage? page) ||
                !page.isFrameCollectionActive)
            {
                return;
            }

            if (previousFrameTimeNanos.HasValue)
            {
                double milliseconds =
                    (frameTimeNanos -
                     previousFrameTimeNanos.Value) /
                    1_000_000.0;

                page.AddFrameTime(milliseconds);
            }

            previousFrameTimeNanos =
                frameTimeNanos;

            if (page.isFrameCollectionActive)
            {
                Choreographer.Instance.PostFrameCallback(
                    this);
            }
        }
    }
#endif
}