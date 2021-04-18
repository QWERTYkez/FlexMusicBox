using System;
using Xamarin.Forms;

namespace FlexMusicBox
{
    public partial class App : Application
    {
        private static Action<Android.Graphics.Color> Act;

        public static event Action MediaNextPressed;
        public static event Action MediaPreviousPressed;

        public static void MediaNext() => MediaNextPressed?.Invoke();
        public static void MediaPrevious() => MediaPreviousPressed?.Invoke();

        public App(Action<Android.Graphics.Color> act)
        {
            Act = act;

            InitializeComponent();

            MainPage = new MainPage();
        }

        public static void SetStatusBarColor(Android.Graphics.Color Color) => Act(Color);

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
