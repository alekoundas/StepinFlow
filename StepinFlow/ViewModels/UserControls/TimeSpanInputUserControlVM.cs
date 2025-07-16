using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace StepinFlow.ViewModels.UserControls
{
    public partial class TimeSpanInputUserControlVM : ObservableObject, INotifyPropertyChanged
    {
        [ObservableProperty]
        private int _hours;

        [ObservableProperty]
        private int _minutes;

        [ObservableProperty]
        private int _seconds;

        [ObservableProperty]
        private int _milliseconds;


        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;


        public int TotalMilliseconds
        {
            get
            {
                // Calculate total milliseconds.
                int total = 0;
                total += Hours * 3600000;   // Hours to milliseconds
                total += Minutes * 60000;   // Minutes to milliseconds
                total += Seconds * 1000;    // Seconds to milliseconds
                total += Milliseconds;        // Milliseconds

                return total;
            }
        }

        public string FormattedTime
        {
            get
            {
                try
                {
                    return $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}.{Milliseconds:D3}";
                }
                catch
                {
                    return "00:00:00.000";
                }
            }
        }

        public TimeSpanInputUserControlVM()
        {
        }


        public void SetFromTotalMilliseconds(int totalMs)
        {
            if (totalMs < 0)
            {
                return;
            }

            var remaining = totalMs;

            Hours = (int)(remaining / 3600000);
            remaining %= 3600000;

            Minutes = (int)(remaining / 60000);
            remaining %= 60000;

            Seconds = (int)(remaining / 1000);
            remaining %= 1000;

            Milliseconds = (int)remaining;
        }
    }
}