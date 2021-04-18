using Android.Media;
using System;
using System.Threading.Tasks;

namespace FlexMusicBox.Player
{
    public class FlexMusicPlayer
    {
        private MediaPlayer _player;
        public FlexMusicPlayer()
        {
            _player = new MediaPlayer();
            _player.BufferingUpdate += (s, e) => CurrentBuffered = e.Percent;
            _player.Prepared += (s, e) => 
            {
                FileDuration = _player.Duration;
                CanSwitch = true;
                DurationAccepted?.
                    Invoke(TimeSpan.FromMilliseconds(_player.Duration));
            };
        }

        public event Action<TimeSpan> DurationAccepted;
        public event Action Finished;
        public event Action<double> Buffered;
        public event Action<TimeSpan> PositionChanged;

        public void PlayNew(Music m)
        {
            var uri = m._GetUri();

            if (uri == null)
            {
                throw new Exception("++++++++++++++++++");
            }
            _player.Reset();
            _player.SetDataSource(Android.App.Application.Context, Android.Net.Uri.Parse(uri.AbsoluteUri));
            _player.Prepare();
            Start();
        }
        public void SeekTo(int seek)
        {
            CanSwitch = true;
            _player.SeekTo(seek);
            PositionChanged?.Invoke(TimeSpan.FromSeconds(seek));
        }
        public async void Start()
        {
            _player.Start();
            if (TrackingPosition) return;
            TrackingPosition = true;
            while (TrackingPosition)
            {
                await Task.Delay(200).ConfigureAwait(false);
                CurrentPosition = _player.CurrentPosition;
            }
        }
        public void Stop()
        {
            TrackingPosition = false;
            _player.Stop();
        }
        public void Pause()
        {
            TrackingPosition = false;
            _player.Pause();
        }

        private double _currentBuffered;
        private double CurrentBuffered
        {
            set
            {
                if (_currentBuffered != value)
                {
                    _currentBuffered = value;
                    Buffered?.Invoke(value / 100);
                }
            }
        }
        private bool CanSwitch = false;
        private int FileDuration;
        private int _currentPosition;
        private int CurrentPosition
        {
            set
            {
                if (CanSwitch && FileDuration - value < 300)
                {
                    CanSwitch = false;
                    Finished?.Invoke();
                }

                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    PositionChanged?.Invoke(TimeSpan.FromMilliseconds(value));
                }
            }
        }
        private bool TrackingPosition = false;
    }
}