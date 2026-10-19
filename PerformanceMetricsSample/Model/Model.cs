namespace PerformanceMetricsSample;

/// <summary>
/// Represents one data point displayed by a chart series.
/// </summary>
public sealed class Model
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    public Model()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class
    /// with the specified X and Y values.
    /// </summary>
    /// <param name="xValue">The horizontal-axis value.</param>
    /// <param name="yValue">The vertical-axis value.</param>
    public Model(double xValue, double yValue)
    {
        XValue = xValue;
        YValue = yValue;
    }

    /// <summary>
    /// Gets or sets the horizontal-axis value.
    /// </summary>
    public double XValue { get; set; }

    /// <summary>
    /// Gets or sets the vertical-axis value.
    /// </summary>
    public double YValue { get; set; }
}



/// <summary>
/// Contains the initial-render and managed-memory measurements.
/// </summary>
public sealed class InitialRenderMeasurement
{
    /// <summary>
    /// Gets the complete initial-render duration in milliseconds.
    /// </summary>
    public double ElapsedMilliseconds { get; init; }

    /// <summary>
    /// Gets the managed-memory value before assigning the chart data.
    /// </summary>
    public long MemoryBeforeBytes { get; init; }

    /// <summary>
    /// Gets the managed-memory value after chart rendering completes.
    /// </summary>
    public long MemoryAfterBytes { get; init; }

    /// <summary>
    /// Gets the positive managed-memory difference in bytes.
    /// </summary>
    public long MemoryDifferenceBytes => Math.Max(0, MemoryAfterBytes - MemoryBeforeBytes);

    /// <summary>
    /// Gets the managed-memory difference in megabytes.
    /// </summary>
    public double MemoryDifferenceMegabytes => MemoryDifferenceBytes / (1024.0 * 1024.0);
}

/// <summary>
/// Stores the numerical results produced by one complete benchmark run.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>
    /// Gets or sets the point count in each individual chart series.
    /// </summary>
    public int PointsPerSeries { get; set; }

    /// <summary>
    /// Gets or sets the number of chart series included in the benchmark.
    /// </summary>
    public int SeriesCount { get; set; }

    /// <summary>
    /// Gets the total number of chart points across all series.
    /// </summary>
    /// <remarks>
    /// The calculation is:
    ///
    /// Series count multiplied by points per series.
    ///
    /// For example, 10 series multiplied by 100,000 points equals
    /// 1,000,000 total points.
    /// </remarks>
    public int TotalPoints => PointsPerSeries * SeriesCount;

    /// <summary>
    /// Gets or sets the initial-render duration in milliseconds.
    /// </summary>
    public double InitialLoadMs { get; set; }

    /// <summary>
    /// Gets or sets the programmatic trackball-response duration
    /// in milliseconds.
    /// </summary>
    public double TrackballMs { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the thirty-step pan scenario
    /// in milliseconds.
    /// </summary>
    public double PanScrollMs { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the fifteen-operation zoom scenario
    /// in milliseconds.
    /// </summary>
    public double ZoomMs { get; set; }

    /// <summary>
    /// Gets or sets the managed-memory difference in megabytes.
    /// </summary>
    public double MemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the average duration of one rendering frame
    /// in milliseconds.
    /// </summary>
    public double AverageUiFrameMs { get; set; }

    /// <summary>
    /// Gets the frame rate derived from the average frame duration.
    /// </summary>
    public double FramesPerSecond => AverageUiFrameMs > 0 ? 1000.0 / AverageUiFrameMs : 0;

    /// <summary>
    /// Gets or sets the number of complete real-time update cycles
    /// performed per second.
    /// </summary>
    /// <remarks>
    /// One update cycle modifies all ten chart series once.
    /// </remarks>
    public double UpdatesPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the actual real-time benchmark duration in seconds.
    /// </summary>
    public double RealTimeDurationSeconds { get; set; }
}

/// <summary>
/// Contains frame intervals collected during the Zoom benchmark.
/// </summary>
public sealed class FrameMeasurement
{
    /// <summary>
    /// Gets the number of valid frame intervals collected.
    /// </summary>
    public int FrameCount { get; init; }

    /// <summary>
    /// Gets the sum of all collected frame intervals.
    /// </summary>
    public double TotalDurationMilliseconds { get; init; }

    /// <summary>
    /// Gets the average interval between consecutive rendering frames.
    /// </summary>
    public double AverageFrameMilliseconds { get; init; }
}