using System.Diagnostics;

#if ANDROID
using Android.Views;
#endif

#if WINDOWS
using Microsoft.UI.Xaml.Media;
#endif

#if IOS || MACCATALYST
using CoreAnimation;
using Foundation;
#endif

namespace PerformanceMetricsSample;

/// <summary>
/// Performs the Syncfusion MAUI chart operations requested by BenchmarkViewModel.
/// </summary>
/// <remarks>
/// The ViewModel controls the benchmark sequence.
///
/// MainPage performs operations that require direct access to the chart:
/// initial rendering, trackball display, zooming, panning, frame waiting,
/// continuous frame measurement, and chart cleanup.
/// </remarks>
public partial class MainPage : ContentPage, IChartBenchmarkHost
{
    /// <summary>
    /// Number of FastLineSeriesExt instances declared in MainPage.xaml.
    /// </summary>
    private const int RequiredSeriesCount = 10;
    /// <summary>
    /// Maximum time allowed for all ten series to report rendering completion.
    /// </summary>
    private const int RenderTimeoutMilliseconds = 30000;
    /// <summary>
    /// Maximum time allowed for the TrackballCreated event.
    /// </summary>
    private const int TrackballTimeoutMilliseconds = 5000;
    /// <summary>
    /// Stores each unique series that completed DrawSeries.
    /// </summary>
    /// <remarks>
    /// A HashSet prevents one series from being counted more than once
    /// when DrawSeries is called repeatedly.
    /// </remarks>
    private readonly HashSet<FastLineSeriesExt> renderedSeries = new();
    /// <summary>
    /// Stores intervals between consecutive platform display frames.
    /// </summary>
    private readonly List<double> frameTimes = new();
    /// <summary>
    /// Synchronizes access to frameTimes.
    /// </summary>
    private readonly object frameLock = new();
    /// <summary>
    /// Completes when all expected series finish their initial drawing operation.
    /// </summary>
    private TaskCompletionSource<bool>? initialRenderCompletionSource;
    /// <summary>
    /// Completes when the chart raises TrackballCreated.
    /// </summary>
    private TaskCompletionSource<bool>? trackballCompletionSource;
    /// <summary>
    /// Number of series expected in the active initial-render benchmark.
    /// </summary>
    private int expectedSeriesCount;
    /// <summary>
    /// Number of points expected in each series.
    /// </summary>
    private int expectedPointCountPerSeries;
    /// <summary>
    /// Indicates whether continuous frame collection is active.
    /// </summary>
    private bool isFrameCollectionActive;
    /// <summary>
    /// Empty data source used when clearing all chart series.
    /// </summary>
    private static readonly IReadOnlyList<Model> EmptyItemsSource = Array.Empty<Model>();

#if ANDROID
    /// <summary>
    /// Continuously records Android Choreographer frame timestamps.
    /// </summary>
    private AndroidContinuousFrameCallback? androidFrameMonitor;
#endif

#if WINDOWS
    /// <summary>
    /// Handles continuous Windows rendering events.
    /// </summary>
    private EventHandler<object>? windowsRenderingHandler;
    /// <summary>
    /// Stores the previous Windows rendering timestamp.
    /// </summary>
    private TimeSpan? previousWindowsRenderingTime;
#endif

#if IOS || MACCATALYST
    /// <summary>
    /// Continuously receives iOS or Mac Catalyst display-refresh callbacks.
    /// </summary>
    private CADisplayLink? appleFrameDisplayLink;
    /// <summary>
    /// Stores the previous Apple display-link timestamp.
    /// </summary>
    private double? previousAppleFrameTimestamp;
#endif

    /// <summary>
    /// Initializes the ViewModel and subscribes to chart events.
    /// </summary>
    public MainPage()
    {
        InitializeComponent();

        // Give the ViewModel access to chart operations through the interface.
        ViewModel.Initialize(this);
        // Monitor rendering completion for all ten series.
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
        // Used by the programmatic trackball response test.
        chart.TrackballCreated += OnTrackballCreated;
    }

    /// <summary>
    /// Loads all ten datasets and measures initial rendering
    /// and managed-memory growth.
    /// </summary>
    /// <param name="datasets">The ten generated data collections.</param>
    /// <param name="expectedSeriesCount">The expected series count.</param>
    /// <param name="expectedPointCountPerSeries">The expected point count in each series.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The initial-render and managed-memory measurement.</returns>
    public async Task<InitialRenderMeasurement> LoadAndRenderAsync(IReadOnlyList<IReadOnlyList<Model>> datasets, int expectedSeriesCount, int expectedPointCountPerSeries, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateDatasets(datasets, expectedSeriesCount, expectedPointCountPerSeries);
        // ClearChart resets the expected values, so it must run first.
        ClearChart();
        this.expectedSeriesCount = expectedSeriesCount;
        this.expectedPointCountPerSeries = expectedPointCountPerSeries;
        // Wait until the removal of the old data reaches a display frame.
        await WaitForRenderFrameAsync(cancellationToken);
        // Match the managed-memory preparation used by the WPF benchmark.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryBefore = GC.GetTotalMemory(true);
        renderedSeries.Clear();
        // OnSeriesRenderCompleted completes this task after all ten
        // unique series have rendered with the expected point count.
        initialRenderCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // Initial-render timing starts immediately before assigning data.
        Stopwatch stopwatch = Stopwatch.StartNew();
        AssignSeriesItemsSources(datasets);
        bool rendered = await WaitForInitialRenderCompletionAsync(cancellationToken);

        if (!rendered)
        {
            stopwatch.Stop();

            throw new TimeoutException($"All {expectedSeriesCount} chart series did not render " + $"within {RenderTimeoutMilliseconds:N0} milliseconds.");
        }

        // Allow the DrawSeries output to reach the next presentation frame.
        await WaitForRenderFrameAsync(cancellationToken);
        stopwatch.Stop();
        long memoryAfter = GC.GetTotalMemory(true);

        return new InitialRenderMeasurement
        {
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            MemoryBeforeBytes = memoryBefore,
            MemoryAfterBytes = memoryAfter
        };
    }

    /// <summary>
    /// Measures programmatic trackball response.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The trackball response duration in milliseconds.</returns>
    public async Task<double> MeasureTrackballAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (benchmarkSeries1.ItemsSource is not IEnumerable<Model> source)
        {
            return 0;
        }

        List<Model> points = source.ToList();
        if (points.Count == 0)
        {
            return 0;
        }

        trackballBehavior.Hide();

        // Ensure the chart is settled before starting measurement.
        await WaitForRenderFrameAsync(cancellationToken);

        int middleIndex = points.Count / 2;
        double middleXValue = points[middleIndex].XValue;
        float xPosition = chart.ValueToPoint(xAxis, middleXValue);
        float yPosition = (float)Math.Max(1, chart.Height / 2);

        if (float.IsNaN(xPosition) || float.IsInfinity(xPosition))
        {
            return 0;
        }

        trackballCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Stopwatch stopwatch = Stopwatch.StartNew();
        // Programmatic display excludes human reaction time.
        trackballBehavior.Show(xPosition, yPosition);
        bool created = await WaitForTrackballCompletionAsync(cancellationToken);
        if (created)
        {
            // Wait until trackball visuals reach the next frame.
            await WaitForRenderFrameAsync(cancellationToken);
        }

        stopwatch.Stop();
        trackballBehavior.Hide();
        return created ? stopwatch.Elapsed.TotalMilliseconds : 0;
    }

    /// <summary>
    /// Waits for the next platform display-frame callback.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the frame wait.</param>
    /// <returns>A task that completes on the next display frame.</returns>
    public Task WaitForRenderFrameAsync(CancellationToken cancellationToken)
    {
        return WaitForNextRenderFrameAsync(cancellationToken);
    }

    /// <summary>
    /// Applies one horizontal Pan operation.
    /// </summary>
    /// <param name="zoomFactor">The proportion of the X axis that is visible.</param>
    /// <param name="zoomPosition">The starting position of the visible range.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A completed task after applying the axis values.</returns>
    public Task ApplyPanAsync(double zoomFactor, double zoomPosition, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        double factor = Math.Clamp(zoomFactor, 0.001, 1.0);
        double maximumPosition = 1.0 - factor;
        xAxis.ZoomFactor = factor;
        xAxis.ZoomPosition = Math.Clamp(zoomPosition, 0, maximumPosition);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Applies one Zoom operation to the X and Y axes.
    /// </summary>
    /// <param name="zoomFactor">The visible proportion of both axes.</param>
    /// <param name="zoomPosition">The X-axis visible-range position.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A completed task after applying the axis values.</returns>
    public Task ApplyZoomAsync(double zoomFactor, double zoomPosition, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        double factor = Math.Clamp(zoomFactor, 0.001, 1.0);
        double maximumPosition = 1.0 - factor;
        xAxis.ZoomFactor = factor;
        xAxis.ZoomPosition = Math.Clamp(zoomPosition, 0, maximumPosition);
        yAxis.ZoomFactor = factor;
        yAxis.ZoomPosition = 0;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts continuous frame-interval collection.
    /// </summary>
    /// <remarks>
    /// Android uses Choreographer.
    /// Windows uses CompositionTarget.Rendering.
    /// iOS and Mac Catalyst use CADisplayLink.
    /// </remarks>
    public void BeginFrameCollection()
    {
        StopFrameCollection();

        lock (frameLock)
        {
            frameTimes.Clear();
        }

        isFrameCollectionActive = true;

#if ANDROID
        androidFrameMonitor = new AndroidContinuousFrameCallback(this);
        Choreographer.Instance?.PostFrameCallback(androidFrameMonitor);

#elif WINDOWS
        previousWindowsRenderingTime = null;
        windowsRenderingHandler = OnWindowsRendering;

        CompositionTarget.Rendering += windowsRenderingHandler;

#elif IOS || MACCATALYST
        previousAppleFrameTimestamp = null;
        appleFrameDisplayLink = CADisplayLink.Create(OnAppleDisplayFrame);
        appleFrameDisplayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
#endif
    }

    /// <summary>
    /// Stops continuous frame monitoring and returns the frame measurements.
    /// </summary>
    /// <returns>The collected frame-count, total-duration, and average-duration values.</returns>
    public FrameMeasurement EndFrameCollection()
    {
        StopFrameCollection();

        lock (frameLock)
        {
            if (frameTimes.Count == 0)
            {
                return new FrameMeasurement
                {
                    FrameCount = 0,
                    TotalDurationMilliseconds = 0,
                    AverageFrameMilliseconds = 0
                };
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
    /// Removes all chart data and active measurement state.
    /// </summary>
    public void ClearChart()
    {
        StopFrameCollection();
        CancelPendingOperations();

        benchmarkSeries1.ItemsSource = EmptyItemsSource;
        benchmarkSeries2.ItemsSource = EmptyItemsSource;
        benchmarkSeries3.ItemsSource = EmptyItemsSource;
        benchmarkSeries4.ItemsSource = EmptyItemsSource;
        benchmarkSeries5.ItemsSource = EmptyItemsSource;
        benchmarkSeries6.ItemsSource = EmptyItemsSource;
        benchmarkSeries7.ItemsSource = EmptyItemsSource;
        benchmarkSeries8.ItemsSource = EmptyItemsSource;
        benchmarkSeries9.ItemsSource = EmptyItemsSource;
        benchmarkSeries10.ItemsSource = EmptyItemsSource;

        renderedSeries.Clear();

        lock (frameLock)
        {
            frameTimes.Clear();
        }

        expectedSeriesCount = 0;
        expectedPointCountPerSeries = 0;

        ResetChart();
    }

    /// <summary>
    /// Validates the number of datasets and the number of points in each dataset.
    /// </summary>
    private static void ValidateDatasets(IReadOnlyList<IReadOnlyList<Model>> datasets, int expectedSeriesCount, int expectedPointCountPerSeries)
    {
        if (expectedSeriesCount != RequiredSeriesCount)
        {
            throw new InvalidOperationException($"This page requires exactly {RequiredSeriesCount} datasets.");
        }

        if (datasets.Count != expectedSeriesCount)
        {
            throw new InvalidOperationException($"Expected {expectedSeriesCount} datasets, " + $"but received {datasets.Count}.");
        }

        for (int index = 0; index < datasets.Count; index++)
        {
            if (datasets[index].Count != expectedPointCountPerSeries)
            {
                throw new InvalidOperationException($"Series {index + 1} contains " + $"{datasets[index].Count:N0} points. " + $"Expected {expectedPointCountPerSeries:N0} points.");
            }
        }
    }

    /// <summary>
    /// Assigns the ten generated datasets to the ten chart series.
    /// </summary>
    private void AssignSeriesItemsSources(IReadOnlyList<IReadOnlyList<Model>> datasets)
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

    /// <summary>
    /// Waits for all expected series to render or for the timeout to expire.
    /// </summary>
    private async Task<bool> WaitForInitialRenderCompletionAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool>? source = initialRenderCompletionSource;

        if (source == null)
        {
            return false;
        }

        Task timeout = Task.Delay(RenderTimeoutMilliseconds,cancellationToken);
        Task completed = await Task.WhenAny(source.Task, timeout);
        cancellationToken.ThrowIfCancellationRequested();
        bool rendered = completed == source.Task;
        initialRenderCompletionSource = null;
        return rendered;
    }

    /// <summary>
    /// Waits for TrackballCreated or for the timeout to expire.
    /// </summary>
    private async Task<bool> WaitForTrackballCompletionAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool>? source = trackballCompletionSource;

        if (source == null)
        {
            return false;
        }

        Task timeout = Task.Delay(TrackballTimeoutMilliseconds, cancellationToken);
        Task completed = await Task.WhenAny(source.Task, timeout);
        cancellationToken.ThrowIfCancellationRequested();
        bool created = completed == source.Task;
        trackballCompletionSource = null;
        return created;
    }

    /// <summary>
    /// Waits for one platform-specific display-frame callback.
    /// </summary>
    private Task WaitForNextRenderFrameAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if ANDROID
        TaskCompletionSource<bool> source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));
        AndroidSingleFrameCallback callback = new(source, registration);
        Choreographer.Instance?.PostFrameCallback(callback);

        return source.Task;

#elif WINDOWS
        TaskCompletionSource<bool> source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<object>? handler = null;
        CancellationTokenRegistration registration = default;

        handler = (sender, arguments) =>
        {
            CompositionTarget.Rendering -= handler;
            registration.Dispose();
            source.TrySetResult(true);
        };

        registration = cancellationToken.Register(() =>
        {
            CompositionTarget.Rendering -= handler;
            source.TrySetCanceled(cancellationToken);
        });

        CompositionTarget.Rendering += handler;
        return source.Task;

#elif IOS || MACCATALYST
        TaskCompletionSource<bool> source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CADisplayLink? displayLink = null;
        CancellationTokenRegistration registration = default;
        displayLink = CADisplayLink.Create(() =>
        {
            displayLink?.Invalidate();
            displayLink?.Dispose();
            displayLink = null;
            registration.Dispose();
            source.TrySetResult(true);
        });

        registration = cancellationToken.Register(() =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                displayLink?.Invalidate();
                displayLink?.Dispose();
                displayLink = null;
                source.TrySetCanceled(cancellationToken);
            });
        });

        displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
        return source.Task;

#else
        TaskCompletionSource<bool> source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(16),
            () => {registration.Dispose();
                source.TrySetResult(true);
            });

        return source.Task;
#endif
    }

    /// <summary>
    /// Stops continuous platform frame monitoring.
    /// </summary>
    private void StopFrameCollection()
    {
        isFrameCollectionActive = false;

#if ANDROID
        if (androidFrameMonitor != null)
        {
            Choreographer.Instance?.RemoveFrameCallback(androidFrameMonitor);
            androidFrameMonitor = null;
        }
#elif WINDOWS
        if (windowsRenderingHandler != null)
        {
            CompositionTarget.Rendering -= windowsRenderingHandler;
            windowsRenderingHandler = null;
        }

        previousWindowsRenderingTime = null;

#elif IOS || MACCATALYST
        if (appleFrameDisplayLink != null)
        {
            appleFrameDisplayLink.Invalidate();
            appleFrameDisplayLink.Dispose();
            appleFrameDisplayLink = null;
        }

        previousAppleFrameTimestamp = null;
#endif
    }

#if WINDOWS
    /// <summary>
    /// Records the interval between consecutive Windows rendering events.
    /// </summary>
    private void OnWindowsRendering(object? sender, object eventArguments)
    {
        if (!isFrameCollectionActive)
        {
            return;
        }

        if (eventArguments is not RenderingEventArgs renderingArguments)
        {
            return;
        }

        TimeSpan currentTime = renderingArguments.RenderingTime;

        if (previousWindowsRenderingTime.HasValue)
        {
            double milliseconds = (currentTime - previousWindowsRenderingTime.Value).TotalMilliseconds;
            AddFrameTime(milliseconds);
        }

        previousWindowsRenderingTime = currentTime;
    }
#endif

#if IOS || MACCATALYST
    /// <summary>
    /// Records the interval between consecutive iOS or Mac Catalyst
    /// display-refresh callbacks.
    /// </summary>
    private void OnAppleDisplayFrame()
    {
        if (!isFrameCollectionActive || appleFrameDisplayLink == null)
        {
            return;
        }

        // CADisplayLink timestamps are expressed in seconds.
        double currentTimestamp = appleFrameDisplayLink.Timestamp;

        if (previousAppleFrameTimestamp.HasValue)
        {
            double milliseconds = (currentTimestamp - previousAppleFrameTimestamp.Value) * 1000.0;
            AddFrameTime(milliseconds);
        }

        previousAppleFrameTimestamp = currentTimestamp;
    }
#endif

    /// <summary>
    /// Adds one valid display-frame interval to the collection.
    /// </summary>
    private void AddFrameTime(double milliseconds)
    {
        if (milliseconds <= 0 || milliseconds >= 1000)
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

    /// <summary>
    /// Counts each unique series after DrawSeries finishes.
    /// </summary>
    private void OnSeriesRenderCompleted(object? sender, EventArgs e)
    {
        if (initialRenderCompletionSource == null || sender is not FastLineSeriesExt series || series.ItemsSource is not ICollection<Model> items || items.Count != expectedPointCountPerSeries)
        {
            return;
        }

        renderedSeries.Add(series);
        if (renderedSeries.Count >= expectedSeriesCount)
        {
            initialRenderCompletionSource.TrySetResult(true);
        }
    }

    /// <summary>
    /// Completes the Trackball benchmark after TrackballCreated.
    /// </summary>
    private void OnTrackballCreated(object? sender, EventArgs e)
    {
        trackballCompletionSource?.TrySetResult(true);
    }

    /// <summary>
    /// Cancels pending initial-render and Trackball waits.
    /// </summary>
    private void CancelPendingOperations()
    {
        initialRenderCompletionSource?.TrySetCanceled();
        trackballCompletionSource?.TrySetCanceled();
        initialRenderCompletionSource = null;
        trackballCompletionSource = null;
        renderedSeries.Clear();
    }

    /// <summary>
    /// Stops frame monitoring and pending operations when the page disappears.
    /// </summary>
    protected override void OnDisappearing()
    {
        StopFrameCollection();
        CancelPendingOperations();
        trackballBehavior.Hide();
        base.OnDisappearing();
    }

#if ANDROID
    /// <summary>
    /// Completes one awaited Android display frame.
    /// </summary>
    private sealed class AndroidSingleFrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
    {
        private readonly TaskCompletionSource<bool> source;
        private readonly CancellationTokenRegistration registration;

        public AndroidSingleFrameCallback(TaskCompletionSource<bool> source, CancellationTokenRegistration registration)
        {
            this.source = source;
            this.registration = registration;
        }

        public void DoFrame(long frameTimeNanos)
        {
            registration.Dispose();
            source.TrySetResult(true);
        }
    }

    /// <summary>
    /// Continuously records Android display-frame intervals.
    /// </summary>
    private sealed class AndroidContinuousFrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
    {
        private readonly WeakReference<MainPage> pageReference;
        private long? previousFrameTimeNanos;

        public AndroidContinuousFrameCallback(MainPage page)
        {
            pageReference = new WeakReference<MainPage>(page);
        }

        public void DoFrame(long frameTimeNanos)
        {
            if (!pageReference.TryGetTarget(out MainPage? page) || !page.isFrameCollectionActive)
            {
                return;
            }

            if (previousFrameTimeNanos.HasValue)
            {
                double milliseconds = (frameTimeNanos - previousFrameTimeNanos.Value) / 1_000_000.0;
                page.AddFrameTime(milliseconds);
            }

            previousFrameTimeNanos = frameTimeNanos;

            if (page.isFrameCollectionActive)
            {
                Choreographer.Instance?.PostFrameCallback(this);
            }
        }
    }
#endif
}