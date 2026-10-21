# Performance Metrics Samples for Syncfusion .NET MAUI Charts

This repository contains two performance-testing samples for the Syncfusion .NET MAUI Cartesian Charts control.

- **Chart Loading Performance Sample** measures the loading time of a `FastLineSeries` with 10K, 50K, and 100K data points.
- **Chart Performance Benchmark Sample** measures Initial Render, Zoom, Pan, Live Update, Trackball Response, UI Response During Zoom, and Managed Memory Usage for 10 `FastLineSeries` instances.

## Samples Included

### 1. Chart Loading Performance Sample

This sample provides three buttons to generate and load:

```text
10K data points
50K data points
100K data points
```

When a button is selected, the sample generates the requested data, binds the collection to a `FastLineSeries`, and displays the elapsed loading time after the series is rendered.

### 2. Chart Performance Benchmark Sample

This sample renders 10 `FastLineSeries` instances. Use the **Points Per Series** picker on `MainPage` to select one of these Load Data values:

```text
5K
10K
50K
75K
100K
```

The selected value represents the number of points in each series.

```text
Total data points = 10 series × selected points per series
```

| Load Data | Series Count | Points Per Series | Total Data Points |
|---|---:|---:|---:|
| 5K | 10 | 5,000 | 50,000 |
| 10K | 10 | 10,000 | 100,000 |
| 50K | 10 | 50,000 | 500,000 |
| 75K | 10 | 75,000 | 750,000 |
| 100K | 10 | 100,000 | 1,000,000 |

The sample reports the following performance metrics:

| Metric | Description |
|---|---|
| Initial Render (ms) | Time from assigning data to all 10 series until all series complete drawing and reach the next presentation frame. |
| Managed Memory Usage (MB) | Difference between managed memory before and after Initial Render. |
| Trackball Response (ms) | Time from the programmatic Trackball request until the Trackball is created and reaches the next presentation frame. |
| Zoom Performance (ms) | Total time required to complete 15 programmatic Zoom operations. |
| Pan Performance (ms) | Total time required to complete 30 programmatic Pan operations. |
| UI Response During Zoom (ms/frame) | Average interval between consecutive native platform display callbacks during Zoom. |
| Updates / Sec | Number of complete live-update cycles processed per second. One cycle updates all 10 series. |

## Steps to Run the Demo

**Step 1:** Clone or download the this repository.

**Step 2:** Open the solution in your preferred IDE.

**Step 3:** Select the required sample project.

**Step 4:** Build and run the project.

### Run the Chart Loading Performance Sample

**Step 5**: Click on the buttons (Load 10K Data, Load 50K Data, Load 100K Data) to generate data. 
> Each button calls the LoadData method of the `DataGenerator` class, which builds a collection of Model objects (XValue, YValue). Values are randomized around a starting point to mimic realistic chart data. Based on the button clicked, [LoadData](https://github.com/SyncfusionExamples/dot_net_MAUI_Chart_Performance_Analysis/blob/3d3619a16c42dc8c914be5778050f793af9c5025/PerformanceMetrics/DataGenerator.cs#L71C9-L71C63) method generates `10000`, `50000`, or `100000` points, then binds them to the chart’s FastLineSeries to measure performance with different dataset sizes..

### Run the Chart Performance Benchmark Sample

All controls and results are available on `MainPage`.

1. Select `5K`, `10K`, `50K`, `75K`, or `100K` from the **Points Per Series** picker.
2. Select **Start Benchmarking**.
3. The sample generates 10 deterministic datasets and runs all benchmark scenarios.
4. Wait until the status changes to **Benchmark completed.**
5. Read the values in the **Performance Metrics** section.
6. Select **Clear / Reset** before starting another run if required.

> The picker value is the point count for each series. For example, selecting 5K renders 10 series × 5,000 points, which equals 50,000 total points.

## Benchmark Sequence

The Chart Performance Benchmark Sample runs the scenarios in this order:

```text
1. Generate 10 deterministic datasets
2. Measure Initial Render and Managed Memory Usage
3. Measure Trackball Response
4. Measure Pan Performance using 30 operations
5. Measure Zoom Performance using 15 operations
6. Measure UI Response During Zoom
7. Measure live-update throughput and duration
```

## Output

### Chart Loading Performance Sample

<img width="1420" height="640" alt="Chart loading performance output" src="https://github.com/user-attachments/assets/d0ce8ef0-8f08-4fd2-9d1e-8ae842bb5ad5" />

### Chart Performance Benchmark Sample

<img width="1895" height="980" alt="image" src="https://github.com/user-attachments/assets/59d13194-27e2-4562-b424-cc9c71d76f41" />
<img width="1903" height="565" alt="image" src="https://github.com/user-attachments/assets/c5a26645-8f45-4e89-9e60-4ce6243dd1ac" />


## Sample Results

### Chart Loading Performance Sample

| Data Size | Time Taken (ms) |
|---|---:|
| 10K | 38.05 |
| 50K | 49.44 |
| 100K | 67.94 |

### Chart Performance Benchmark Sample

The following example values are shown for the 5K selection, which represents 10 series × 5,000 points = 50,000 total points.

| Load Data | Initial Render (ms) | Trackball (ms) | Zoom (ms) | Pan (ms) | UI Response During Zoom (ms/frame) | Updates / Sec |
|---|---:|---:|---:|---:|---:|---:|
| 5K | 177 | 44.96 | 277 | 1332 | 18.98 | 27.26 |
| 10K | 154 | 40.61 | 582 | 1362 | 48.60 | 	20.81 |
| 50K |244 | 40.14 | 682 | 1988 | 59.70 | 14.68 |
| 75K | 315 | 53.02 | 755 | 2165 | 66.10 | 11.85 |
| 100K | 382 | 60.43 | 832 | 2082 | 66.23 | 12.25 |

## Specifications

| Specification | Value |
|---|---|
| .NET version | .NET 10.0 |
| Syncfusion Charts version | 31.2.15 |
| Build configuration | Release |
| RAM | 16 GB |

### Guidelines to Optimize Performance in Syncfusion® MAUI Charts
1. **Disable Anti-Aliasing**
    - Description: Anti-aliasing smooths out jagged edges on lines but can increase rendering time.
    - Implementation: Set the [EnableAntiAliasing](https://help.syncfusion.com/cr/maui/Syncfusion.Maui.Charts.FastLineSeries.html#Syncfusion_Maui_Charts_FastLineSeries_EnableAntiAliasing) property to false.
    - Benefit: Disabling anti-aliasing reduces the rendering overhead, improving performance without significantly compromising visual quality.
    ```xml
    <chart:FastLineSeries  
                    ItemsSource="{Binding DataCollection}"
                    XBindingPath="Date"
                    YBindingPath="Value"
                    EnableAntiAliasing="False">
    </chart:FastLineSeries> 
    ```  

2.  **Reduce Stroke Width**
    - Description: Thicker lines require more rendering effort.
    - Implementation: Reduce the series [StrokeWidth](https://help.syncfusion.com/cr/maui/Syncfusion.Maui.Charts.XYDataSeries.html#Syncfusion_Maui_Charts_XYDataSeries_StrokeWidth) property to 1.
    - Benefit: Using a thinner line ensures faster rendering while retaining clarity in the chart.
    ```xml
    <chart:FastLineSeries 
                    ItemsSource="{Binding DataCollection}"
                    XBindingPath="Date"
                    StrokeWidth="1"
                    YBindingPath="Value">
    </chart:FastLineSeries> 
    ```

3. **Remove Data Labels**
    - Description: Displaying data labels for each data point can significantly slow down rendering, especially with large datasets.
    - Implementation: Avoid using data labels with the FastLine series. Set the [ShowDataLabels](https://help.syncfusion.com/cr/maui/Syncfusion.Maui.Charts.ChartSeries.html#Syncfusion_Maui_Charts_ChartSeries_ShowDataLabels) property to false.
    - Benefit: Eliminating data labels speeds up rendering and reduces visual clutter in high-density charts.
    ```xml
    <chart:FastLineSeries 
                    ItemsSource="{Binding DataCollection}"
                    XBindingPath="Date"
                    ShowDataLabels="False"
                    YBindingPath="Value">
    </chart:FastLineSeries> 
    ```

4. **Choose the Appropriate Axis**
    - Description: The choice of axis affects rendering performance. A CategoryAxis processes each label individually, which can be slower compared to NumericalAxis or DateTimeAxis.
    - Implementation: Use DateTimeAxis or NumericalAxis based on the data type.
    - Benefit: These axes streamline data processing, resulting in faster chart updates.
    ```xml
    <chart:SfCartesianChart.XAxes>
        <chart:DateTimeAxis>
        </chart:DateTimeAxis>
    </chart:SfCartesianChart.XAxes>

    <chart:SfCartesianChart.YAxes>
        <chart:NumericalAxis>
        </chart:NumericalAxis>
    </chart:SfCartesianChart.YAxes> 
    ```

By following these strategies, you can optimize the performance of the [FastLine Series](https://help.syncfusion.com/maui/cartesian-charts/fastline) in .NET MAUI Cartesian Chart Control.

## Troubleshooting

### Path Too Long Exception

If a path-too-long exception occurs while building the sample, close the IDE, move or rename the repository to a shorter path, reopen the project, and rebuild it.
