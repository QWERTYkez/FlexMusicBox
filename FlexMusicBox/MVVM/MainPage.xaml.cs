using System;
using System.Globalization;
using System.Threading.Tasks;
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

            VM.Animation1 = () => Loading1.RelRotateTo(900, 5000);
            VM.Animation2 = () => Loading2.RelRotateTo(900, 5000);

            VM.Dispatcher = this.Dispatcher;
            VM.MusicsListView = this.MusicsListView;

            this.Appearing += FirstAppearing;

            this.TrackSlider.DragStarted += (s, e) =>
            {
                VM.DurationSliderLock = true;
                this.TrackSlider.ValueChanged += TrackSlider_ValueChanged;
                TrackSliderGrd.IsVisible = true;
            };
            this.TrackSlider.DragCompleted += (s, e) =>
            {
                this.TrackSlider.ValueChanged -= TrackSlider_ValueChanged;
                TrackSliderGrd.IsVisible = false;
                MainPageViewModel._MP.SeekTo((int)TrackSlider.Value * 1000);
                VM.DurationSliderLock = false;
            };
            this.DurationGRD.SizeChanged += (s, e) => VM.DurationGRDSize = this.DurationGRD.Width;
            this.MusicsScrollView.SizeChanged += (s, e) => VM.VkPlaylistGrdHeight = this.MusicsScrollView.Height - 22;

            VM.Scrolled += async dY => await MusicsScrollView.ScrollToAsync(0, MusicsScrollView.ScrollY + dY, false);
        }

        private void TrackSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            TrackSliderLabel.Text = $"{TimeSpan.FromSeconds(e.NewValue):m\\:ss}";
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
            return $"{TimeSpan.FromSeconds((double)value):m\\:ss}";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((TimeSpan)value).TotalSeconds;
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
    public class BoolToBrushConverter : IValueConverter
    {
        private static Brush On = new SolidColorBrush(Color.FromHex("#50FFFFFF"));
            private static Brush Off = new SolidColorBrush(Color.FromHex("#50000000"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value) return On;
            else return Off;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}