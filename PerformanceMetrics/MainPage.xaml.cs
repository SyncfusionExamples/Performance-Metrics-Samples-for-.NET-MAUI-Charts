using System.Collections.ObjectModel;
using Syncfusion.Maui.Charts;

namespace PerformanceMetrics
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.BindingContext is DataGenerator generator)
            {
                ObservableCollection<Model> data = new ObservableCollection<Model>();
                data = generator.LoadData(10000);
                generator.IsActive = true;
                generator.StartTime = DateTime.Now;
                generator.IsRunning = true;
                mySeries1.ItemsSource = data;
            }
        }

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.BindingContext is DataGenerator generator)
            {
                ObservableCollection<Model> data = new ObservableCollection<Model>();
                data = generator.LoadData(50000);
                generator.IsActive = true;
                generator.StartTime = DateTime.Now;
                generator.IsRunning = true;
                mySeries1.ItemsSource = data;
            }
        }

        private void Button_Clicked_2(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.BindingContext is DataGenerator generator)
            {
                ObservableCollection<Model> data = new ObservableCollection<Model>();
                data = generator.LoadData(100000);
                generator.IsActive = true;
                generator.StartTime = DateTime.Now;
                generator.IsRunning = true;
                mySeries1.ItemsSource = data;
            }

        }
    }

    public class FastLineSeriesExt : FastLineSeries
    {
        protected override void DrawSeries(ICanvas canvas, ReadOnlyObservableCollection<ChartSegment> segments, RectF clipRect)
        {
            base.DrawSeries(canvas, segments, clipRect);

            if (this.BindingContext is DataGenerator generator)
            {
                if (generator.IsActive)
                {
                    TimeSpan timeTaken = DateTime.Now.Subtract(generator.StartTime);
                    generator.IsActive = false;
                    generator.TimeTaken = "Time Taken: " + timeTaken.TotalMilliseconds.ToString() + "ms";
                    generator.IsRunning = false;
                }
            }
        }
    }
}
