using System;
using System.Globalization;
using VkNet.Model.Attachments;
using Xamarin.Forms;

namespace FlexMusicBox
{
    public partial class MainPage : ContentPage
    {
        MainPageViewModel VM;

        public MainPage()
        {
            InitializeComponent();

            VM = this.BindingContext as MainPageViewModel;
            var grd = new Grid();

            this.Appearing += FirstAppearing;
        }

        void FirstAppearing(object sender, EventArgs e)
        {
            this.Appearing -= FirstAppearing;
            VM.FirstAppearing();
        }

        private void ScrollView_Scrolled(object sender, ScrolledEventArgs e) => 
            VM.Scrolled(((ScrollView)sender).Height + e.ScrollY - 22);
    }

    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"{new TimeSpan((int)value * 10000000):mm\\:ss}";
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