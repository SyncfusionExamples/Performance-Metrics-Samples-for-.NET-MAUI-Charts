# Performance Metrics Samples for Syncfusion .NET MAUI Charts

This repository contains two performance-testing samples for the Syncfusion .NET MAUI Cartesian Charts control:

1. **Chart Loading Performance Sample**
2. **Chart Performance Benchmark Sample**

---

## Sample 1: Chart Loading Performance

It includes three buttons to generate and load different amounts of data: 10K, 50K, and 100K, respectively. Upon generating the data, a timer starts and records the elapsed time once the series is rendered. Here utilized the FastLineSeries of SfCartesianChart. This sample allows you to understand the time taken to load varying amounts of data in .NET MAUI charts.

### Steps to Run the Chart Loading Performance Sample

**Step 1:** Clone or download this repository.

**Step 2:** Open the PerformanceMetrics solution in your preferred IDE.

**Step 3:** Build and run the project.

**Step 4:** Select **Load 10K Data**, **Load 50K Data**, or **Load 100K Data**.

Each button calls the `LoadData` method of the `DataGenerator` class. The method creates a collection of `Model` objects with randomized `XValue` and `YValue` values, then binds the collection to the chart's `FastLineSeries`.

**Step 6:** Read the elapsed loading time displayed after the series finishes rendering.

### Chart Loading Performance Output

<img width="1420" height="640" alt="Chart loading performance output" src="https://github.com/user-attachments/assets/d0ce8ef0-8f08-4fd2-9d1e-8ae842bb5ad5" />

### Chart Loading Performance Results

| Data Size | Time Taken (ms) |
|---|---:|
| 10K | 38.05 |
| 50K | 49.44 |
| 100K | 67.94 |

---

## Sample 2: Chart Performance Benchmark

The Chart Performance Benchmark Sample measures rendering, interaction, live-update, UI-response, and managed-memory performance using 10 `FastLineSeries` instances.

### Load Data Options

Use the **Points Per Series** picker to select one of the following values:

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

### Performance Metrics

| Metric | Description |
|---|---|
| Initial Render (ms) | Time from assigning data to all 10 series until all series complete drawing and reach the next presentation frame. |
| Managed Memory Usage (MB) | Difference between managed memory before and after Initial Render. |
| Trackball Response (ms) | Time from the programmatic Trackball request until the Trackball is created and reaches the next presentation frame. |
| Zoom Performance (ms) | Total time required to complete 15 programmatic Zoom operations. |
| Pan Performance (ms) | Total time required to complete 30 programmatic Pan operations. |
| UI Response During Zoom (ms/frame) | Average interval between consecutive native platform display callbacks during Zoom. |
| Updates / Sec | Number of complete live-update cycles processed per second. One cycle updates all 10 series. |
| Real-Time Duration (sec) | Actual elapsed duration of the live-update test. |

### Steps to Run the Chart Performance Benchmark Sample

**Step 1:** Clone or download this repository.

**Step 2:** Open the PerformanceMetricsSample solution in your preferred IDE.

**Step 3:** Build and run the project.

**Step 4:** Select `5K`, `10K`, `50K`, `75K`, or `100K` from the **Points Per Series** picker.

**Step 5:** Select **Start Benchmarking**.

**Step 6:** Wait until the status changes to **Benchmark completed.**

**Step 7:** Read the measured values in the **Performance Metrics** section.

**Step 8:** Select **Clear / Reset** before starting another benchmark run, if required.

### Benchmark Sequence

The sample runs the scenarios in the following order:

```text
1. Generate 10 deterministic datasets
2. Measure Initial Render and Managed Memory Usage
3. Measure Trackball Response
4. Measure Pan Performance using 30 operations
5. Measure Zoom Performance using 15 operations
6. Measure UI Response During Zoom
7. Measure live-update throughput and duration
```

### Chart Performance Benchmark Output

<img width="1895" height="980" alt="Chart Performance Benchmark chart and controls" src="https://github.com/user-attachments/assets/59d13194-27e2-4562-b424-cc9c71d76f41" />

<img width="1903" height="565" alt="Chart Performance Benchmark metrics" src="https://github.com/user-attachments/assets/c5a26645-8f45-4e89-9e60-4ce6243dd1ac" />

### Chart Performance Benchmark Results

The following results use 10 series. The selected Load Data value represents the number of points in each series.

| Load Data | Initial Render (ms) | Trackball (ms) | Zoom (ms) | Pan (ms) | UI Response During Zoom (ms/frame) | Updates / Sec |
|---|---:|---:|---:|---:|---:|---:|
| 5K | 177 | 44.96 | 277 | 1,332 | 18.98 | 27.26 |
| 10K | 154 | 40.61 | 582 | 1,362 | 48.60 | 20.81 |
| 50K | 244 | 40.14 | 682 | 1,988 | 59.70 | 14.68 |
| 75K | 315 | 53.02 | 755 | 2,165 | 66.10 | 11.85 |
| 100K | 382 | 60.43 | 832 | 2,082 | 66.23 | 12.25 |

---

## Specifications

| Specification | Value |
|---|---|
| .NET version | .NET 10.0 |
| Syncfusion Charts version | latest version |
| Build configuration | Release |
| RAM | 16 GB |

## Guidelines to Optimize Performance in Syncfusion .NET MAUI Charts

### 1. Disable Anti-Aliasing

Anti-aliasing smooths line edges but can increase rendering time. Set `EnableAntiAliasing` to `False` when smooth edges are not required.

```xml
<chart:FastLineSeries
    ItemsSource="{Binding DataCollection}"
    XBindingPath="XValue"
    YBindingPath="YValue"
    EnableAntiAliasing="False" />
```

### 2. Reduce Stroke Width

Thicker lines require more rendering work. Use a smaller stroke width when appropriate.

```xml
<chart:FastLineSeries
    ItemsSource="{Binding DataCollection}"
    XBindingPath="XValue"
    YBindingPath="YValue"
    StrokeWidth="1" />
```

### 3. Remove Data Labels

Data labels add visual elements for every data point and can increase rendering time for large datasets.

```xml
<chart:FastLineSeries
    ItemsSource="{Binding DataCollection}"
    XBindingPath="XValue"
    YBindingPath="YValue"
    ShowDataLabels="False" />
```

### 4. Choose the Appropriate Axis

Use a `NumericalAxis` or `DateTimeAxis` when the data is numeric or time-based. A `CategoryAxis` processes individual category labels and can add overhead for large datasets.

```xml
<chart:SfCartesianChart.XAxes>
    <chart:NumericalAxis />
</chart:SfCartesianChart.XAxes>

<chart:SfCartesianChart.YAxes>
    <chart:NumericalAxis />
</chart:SfCartesianChart.YAxes>
```

## Troubleshooting

### Path Too Long Exception

If a path-too-long exception occurs while building the sample, close the IDE, move or rename the repository to a shorter path, reopen the project, and rebuild it.
