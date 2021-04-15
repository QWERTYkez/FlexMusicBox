using Android.App;
using Android.Content.PM;
using Android.Media;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Util;
using MediaManager;
using System.Threading.Tasks;

namespace FlexMusicBox.Droid
{
    [Activity(Label = "FlexMusicBox", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //var CMM = new CrossMediaManager222();
            //CMM.Current.Init(this);
            
            //MediaManager.CrossMediaManager.Current.Init(this);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            initFontScale();
            LoadApplication(new App());
        }

        private void initFontScale()
        {
            var configuration = Resources.Configuration;
            configuration.FontScale = (float)1;
            //0.85 small, 1 standard, 1.15 big，1.3 more bigger ，1.45 supper big 
            var metrics = new DisplayMetrics();
            WindowManager.DefaultDisplay.GetMetrics(metrics);
            metrics.ScaledDensity = configuration.FontScale * metrics.Density;
            BaseContext.Resources.UpdateConfiguration(configuration, metrics);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}