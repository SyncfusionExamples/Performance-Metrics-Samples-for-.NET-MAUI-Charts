namespace PerformanceMetricsSample;

/// <summary>
/// Represents one data point displayed by a chart series.
/// </summary>
public sealed class Model
{
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