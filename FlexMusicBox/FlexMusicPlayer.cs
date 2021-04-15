using Android.Media;
using Plugin.SimpleAudioPlayer;
using System;
using System.Collections.Generic;
using System.IO;
using Android.Net;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using static Android.Media.MediaPlayer;
using System.Threading;

namespace FlexMusicBox.Player
{
    public class FlexMusicPlayer
    {
        public FlexMusicPlayer()
        {
            
        }

        public void Play(System.Uri uri)
        {

            var zz = uri.ToString();

            var player = new MediaPlayer();
            player.BufferingUpdate += (s, e) =>
            {
                //Debug.WriteLine($" BufferingUpdate {e.Percent}");
            };
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    Debug.WriteLine($"+++++ CurrentPosition {TimeSpan.FromMilliseconds(player.CurrentPosition):m\\:ss}");
                }
            });
            player.SetDataSource(Android.App.Application.Context, Android.Net.Uri.Parse(zz));
            player.Prepare();
            player.Start();
        }
    }
}
