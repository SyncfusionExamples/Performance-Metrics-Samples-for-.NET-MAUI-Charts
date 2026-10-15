namespace PerformanceMetricsSample;

/// <summary>
/// Defines the chart operations required by BenchmarkViewModel.
/// </summary>
public interface IChartBenchmarkHost
{
    /// <summary>
    /// Loads all generated datasets and measures initial rendering
    /// and managed-memory usage.
    /// </summary>
    Task<InitialRenderMeasurement> LoadAndRenderAsync(
        IReadOnlyList<IReadOnlyList<Model>> datasets,
        int expectedSeriesCount,
        int expectedPointCountPerSeries,
        CancellationToken cancellationToken);

    /// <summary>
    /// Measures the programmatic Trackball response time.
    /// </summary>
    Task<double> MeasureTrackballAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the next platform display frame.
    /// </summary>
    Task WaitForRenderFrameAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies one horizontal Pan operation.
    /// </summary>
    Task ApplyPanAsync(
        double zoomFactor,
        double zoomPosition,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies one Zoom operation to the chart.
    /// </summary>
    Task ApplyZoomAsync(
        double zoomFactor,
        double zoomPosition,
        CancellationToken cancellationToken);

    /// <summary>
    /// Starts continuous frame-interval collection.
    /// </summary>
    void BeginFrameCollection();

    /// <summary>
    /// Stops frame collection and returns the frame measurement.
    /// </summary>
    FrameMeasurement EndFrameCollection();

    /// <summary>
    /// Restores the unzoomed chart state.
    /// </summary>
    void ResetChart();

    /// <summary>
    /// Removes existing chart data and cancels pending chart operations.
    /// </summary>
    void ClearChart();
}