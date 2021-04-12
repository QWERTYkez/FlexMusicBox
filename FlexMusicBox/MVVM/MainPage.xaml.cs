using System;
using System.Globalization;
using VkNet.Model.Attachments;
using Xamarin.Forms;

namespace FlexMusicBox
{
    public partial class MainPage : ContentPage
    {
        MainPageViewModel VM { get => this.BindingContext as MainPageViewModel; }

        public MainPage()
        {
            InitializeComponent();

            var grd = new Grid();

            VM.Dispatcher = this.Dispatcher;
            VM.MusicsListView = this.MusicsListView;

            this.Appearing += FirstAppearing;

            this.TrackSlider.DragStarted += (s, e) =>
            {
                VM.DurationSliderLock = true;
                this.TrackSlider.ValueChanged += TrackSlider_ValueChanged;
            };
            this.TrackSlider.DragCompleted += (s, e) =>
            {
                this.TrackSlider.ValueChanged -= TrackSlider_ValueChanged;
                TrackSliderLabel.Text = "";
                MainPageViewModel._MM.SeekTo(new TimeSpan(Convert.ToInt32(TrackSlider.Value) * 10000000));
                VM.DurationSliderLock = false;
            };
            this.DurationGRD.SizeChanged += (s, e) => VM.DurationGRDSize = this.DurationGRD.Width;
            this.VkPlaylistGrd.SizeChanged += (s, e) => VM.VkPlaylistGrdHeight = this.VkPlaylistGrd.Height - 22;

            VM.Scrolled += async dY => await MusicsScrollView.ScrollToAsync(0, MusicsScrollView.ScrollY + dY, false);
        }

        private void TrackSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            TrackSliderLabel.Text = $"{new TimeSpan(Convert.ToInt32(e.NewValue) * 10000000):m\\:ss}";
        }

        void FirstAppearing(object sender, EventArgs e)
        {
            this.Appearing -= FirstAppearing;
            VM.FirstAppearing();
        }

        private void ScrollView_Scrolled(object sender, ScrolledEventArgs e) => 
            VM.Scrolling(((ScrollView)sender).Height + e.ScrollY - 22);
    }

    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"{new TimeSpan(System.Convert.ToInt32(value) * 10000000):m\\:ss}";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(((TimeSpan)value).TotalSeconds);
        }
    }
    public class GridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((double)value < 0)
                return new GridLength(0);
            else
                return new GridLength((double)value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((GridLength)value).Value;
        }
    }
    public class AudioNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var val = value as Audio;
                return $"{val.Artist} - {val.Title}";
            }
            else return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception();
        }
    }
}