using Syncfusion.Maui.Charts;

using System.Collections.ObjectModel;
namespace PerformanceMetricsSample;

/// <summary>
/// Extends FastLineSeries so the benchmark can detect DrawSeries completion.
/// </summary>
public sealed class FastLineSeriesExt : FastLineSeries
{
    /// <summary>
    /// Occurs after DrawSeries finishes.
    /// </summary>
    public event EventHandler? RenderCompleted;

    protected override void DrawSeries(ICanvas canvas,  ReadOnlyObservableCollection<ChartSegment> segments, RectF clipRect)
    {
        base.DrawSeries( canvas,  segments,clipRect);

        RenderCompleted?.Invoke(this, EventArgs.Empty);
    }
}