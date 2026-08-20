using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PerformanceMetrics
{
    public class Model
    {
        public double XValue { get; set; }
        public double YValue { get; set; }
    }

    public class DataGenerator : INotifyPropertyChanged
    {
        public DateTime StartTime { get; set; }

        public bool IsRunning { get; set; }

        public string timeTaken = "Time Taken: 0 ms";

        private bool isActive = false;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (isActive != value)
                {
                    isActive = value;
                    OnPropertyChanged(nameof(IsActive));
                }
            }
        }

        private bool isVisible = false;

        public bool IsVisible
        {
            get { return !IsActive; }
            set
            {
                if (isVisible != value)
                {
                    isVisible = value;
                    OnPropertyChanged(nameof(IsVisible));
                }
            }
        }

        public string TimeTaken
        {
            get
            {
                return timeTaken;
            }
            set
            {
                timeTaken = value;
                OnPropertyChanged(nameof(TimeTaken));
            }
        }

        Random random;

        public DataGenerator()
        {
            random = new Random();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Model> LoadData(int count)
        {
            ObservableCollection<Model> data = new ObservableCollection<Model>();

            double value = 50;

            for (int i = 0; i < count; i++)
            {
                data.Add(new Model() { XValue = i, YValue = value });

                if (random.NextDouble() > .5)
                {
                    value += random.NextDouble();
                }
                else
                {
                    value -= random.NextDouble();
                }
            }

            return data;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
