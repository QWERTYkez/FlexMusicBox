using Android.App;
using Android.Content;
using Android.Views;

namespace FlexMusicBox.Droid
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionMediaButton })]
    public class MyMediaButtonBroadcastReceiver : BroadcastReceiver
    {
        public string ComponentName { get { return Class.Name; } }

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Intent.ActionMediaButton)
                return;

            switch (((KeyEvent)intent.GetParcelableExtra(Intent.ExtraKeyEvent)).KeyCode)
            {
                case Keycode.MediaNext:
                    App.MediaNext();
                    break;
                case Keycode.MediaPrevious:
                    App.MediaPrevious();
                    break;
            }
        }
    }
}