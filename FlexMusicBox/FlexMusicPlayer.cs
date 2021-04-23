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

        public void PlayNew(string uri, int mseek = -1)
        {
            _player.Reset();
            _player.SetDataSource(Android.App.Application.Context, Android.Net.Uri.Parse(uri));
            _player.Prepare();
            Start(mseek);
        }
        public void SeekTo(int mseek)
        {
            CanSwitch = true;
            _player.SeekTo(mseek);
            PositionChanged?.Invoke(TimeSpan.FromMilliseconds(mseek));
        }
        public async void Start(int mseek = -1)
        {
            _player.Start();
            if (mseek > 0) SeekTo(mseek);
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

    public class StreamMediaDataSource : MediaDataSource
    {
        System.IO.Stream data;

        public StreamMediaDataSource(System.IO.Stream Data)
        {
            data = Data;
        }

        public override long Size
        {
            get
            {
                return data.Length;
            }
        }

        public override int ReadAt(long position, byte[] buffer, int offset, int size)
        {
            data.Seek(position, System.IO.SeekOrigin.Begin);
            return data.Read(buffer, offset, size);
        }

        public override void Close()
        {
            if (data != null)
            {
                data.Dispose();
                data = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (data != null)
            {
                data.Dispose();
                data = null;
            }
        }
    }
}