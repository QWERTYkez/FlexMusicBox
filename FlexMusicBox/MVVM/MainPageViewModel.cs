using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VkNet;
using VkNet.AudioBypassService.Exceptions;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using Xamarin.Forms;
using DM = FlexMusicBox.UserDataManager;

namespace FlexMusicBox
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        const int MaxDispQuantity = 50;
        const int RowHeight = 48;

        public static MediaManager.IMediaManager _MM { get => MediaManager.CrossMediaManager.Current; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public event Action<int> Scrolled;
        public IDispatcher Dispatcher;

        public static readonly VkApi _vk;
        static MainPageViewModel()
        {
            var services = new ServiceCollection();
            services.AddAudioBypass();
            _vk = new VkApi(services);
        }

        public Command Cmd_VkAuth { get; set; }
        public Command Cmd_VkShowAll { get; set; }
        public Command Cmd_VkSelectPlaylist { get; set; }
        public Command Cmd_Play { get; set; }
        public Command Cmd_Pause { get; set; }
        public Command Cmd_Next { get; set; }
        public Command Cmd_Previous { get; set; }
        public Command Cmd_ToAudioList { get; set; }
        public Command Cmd_ToPlayer { get; set; }
        public MainPageViewModel()
        {
            Cmd_VkAuth = new Command(() =>
            {
                Task.Run(async () =>
                {
                    VkAuthInfo = "Попытка входа...";
                    var Login = VkLogin;
                    var Pass = VkPass;

                    try
                    {
                        _vk.Authorize(new ApiAuthParams
                        {
                            Login = Login,
                            Password = Pass
                        });
                    }
                    catch (VkAuthException ex)
                    {
                        VkAuthInfo = ex.Message;
                        return;
                    }

                    DM.SaveProps(new Props
                    {
                        VkUserAuth = new VkUserAuth(Login, Pass, _vk.UserId.Value),
                        VkToken = _vk.Token
                    });

                    VkAuthInfo = "Авторизация пройдена";

                    await Task.Delay(1000);
                    VkOpened();
                });
            });
            Cmd_VkShowAll = new Command(() => 
            {
                SelectedPlaylist = null;
                SelectedPlaylist = DefaultPlaylist;
            });
            Cmd_Play = new Command(Play);
            Cmd_Pause = new Command(Pause);
            Cmd_Next = new Command(() => Music.CurrentPlayingMusic?.PlayNext());
            Cmd_Previous = new Command(() => Music.CurrentPlayingMusic?.PlayPrevious());
            Cmd_ToAudioList = new Command(() =>
            {
                VkPlaylistGrdIsVisible = true;
                PlayerGrdIsVisible = false;
            });
            Cmd_ToPlayer = new Command(() =>
            {
                VkPlaylistGrdIsVisible = false;
                PlayerGrdIsVisible = true;
            });

            _MM.BufferedChanged += _MM_BufferedChanged;
            _MM.PositionChanged += _MM_PositionChanged;
            _MM.StateChanged += _MM_StateChanged;
        }
        public Task FirstAppearing()
        {
            return Task.Run(() =>
            {
                _vk.OnTokenExpires += s =>
                {
                    _vk.RefreshToken();
                    DM.VkToken = _vk.Token;
                };
                _vk.OnTokenUpdatedAutomatically += s => DM.VkToken = _vk.Token;

                ApiAuthParams Ap = null;
                if (DM.Get_VkUserAuth(out var UserAuth))
                {
                    Ap = UserAuth.ApiAuthParams;
                    VkLogin = UserAuth.Login;
                    VkPass = UserAuth.Pass;
                }
                if (DM.Get_VkToken(out var token))
                {
                    try
                    {
                        _vk.Authorize(new ApiAuthParams { AccessToken = token, UserId = Ap.UserId });
                        VkOpened();
                        return;
                    }
                    catch
                    {
                        if (Ap != null)
                        {
                            _vk.Authorize(Ap);
                            DM.VkToken = _vk.Token;
                            VkOpened();
                            return;
                        }
                    }
                }
                else
                {
                    if (Ap != null)
                    {
                        _vk.Authorize(Ap);
                        DM.VkToken = _vk.Token;
                        VkOpened();
                        return;
                    }
                }
                VkAuthGrdIsVisible = true;
            });
        }
        void VkOpened()
        {
            VkAuthGrdIsVisible = false;
            VkPlaylistGrdIsVisible = true;
            Playlists = new ObservableCollection<Playlist>(_vk.Audio.GetPlaylists(_vk.UserId.Value).Select(p => new Playlist(p)));
            DefaultPlaylist = new Playlist();
            SelectedPlaylist = DefaultPlaylist;

            RestorePlaying();
        }
        void RestorePlaying()
        {
            if (DM.Get_VkPlayerPosition(out var pp))
            {
                var pls = Playlist.AllPlaylists.Where(p => p.Id == pp.PlaylistId).ToList();
                if (pls.Count > 0)
                {
                    var msics = pls.First().Musics;
                    if (msics.Count > pp.MusicIndex)
                    {
                        msics[pp.MusicIndex].Play();
                        PlayerGrdIsVisible = true;
                    }
                }
            }
        }

        bool Scroll = false;
        public double VkPlaylistGrdHeight;
        public void Scrolling(double height)
        {
            if (height >= MaxDispQuantity * RowHeight)
            {
                if (!Scroll && _skip < _MaxSkip)
                {
                    Scroll = true;
                    {
                        var New = SelectedPlaylist._Get(_skip + MaxDispQuantity, (MaxDispQuantity / 2));

                        Dispatcher.BeginInvokeOnMainThread(() => 
                        {
                            for (int i = 0; i < New.Count; i++)
                            {
                                VkAudios.Add(New[i]);
                                VkAudios.RemoveAt(0);
                            }
                            Task.Run(() => 
                            {
                                Scrolled(-New.Count * RowHeight);

                                _skip += New.Count;

                                Scroll = false;
                            });
                            ShowPlayedMusic();
                        });
                    }
                }
            }
            else if (height == VkPlaylistGrdHeight)
            {
                if (!Scroll && _skip > 0)
                {
                    Scroll = true;
                    {
                        var New = SelectedPlaylist._Get(_skip - MaxDispQuantity, (MaxDispQuantity / 2));

                        Dispatcher.BeginInvokeOnMainThread(() =>
                        {
                            var j = VkAudios.Count;
                            for (int i = New.Count - 1; i > -1; i--)
                            {
                                VkAudios.Insert(0, New[i]);
                                VkAudios.RemoveAt(j);
                            }
                            Task.Run(() =>
                            {
                                Scrolled(New.Count * RowHeight);

                                _skip -= New.Count;

                                Scroll = false;
                            });
                            ShowPlayedMusic();
                        });
                    }
                }
            }
        }

        public string VkLogin { get; set; }
        public string VkPass { get; set; }
        public string VkAuthInfo { get; set; }

        public bool VkAuthGrdIsVisible { get; set; } = false;
        public bool VkPlaylistGrdIsVisible { get; set; } = false;
        public bool PlayerGrdIsVisible { get; set; } = false;      ////////////

        public bool PlayButtonIsVisible { get; set; } = false;
        public bool PauseButtonIsVisible { get; set; } = true;

        public ObservableCollection<Playlist> Playlists { get; set; }
        private Playlist DefaultPlaylist;
        private Playlist _sap;
        private int _skip;
        private int _MaxSkip;
        public Playlist SelectedPlaylist
        {
            get => _sap;
            set
            {
                _sap = value;
                VkAudios = null;
                Task.Run(async () =>
                {
                    VkAudios = new ObservableCollection<Music>(value._Get(0, MaxDispQuantity));
                    _skip = 0;
                    _MaxSkip = value.Count - MaxDispQuantity;

                    ShowPlayedMusic();
                });
            }
        }
        public double AudiosListHeight { get; set; }
        private ObservableCollection<Music> _VkAudios { get; set; }
        public ObservableCollection<Music> VkAudios 
        {
            get => _VkAudios;
            set
            {
                _VkAudios = value;
                if (value != null)
                    AudiosListHeight = value.Count * RowHeight;
            }
        }
        private Music _sad;
        private bool AutoAudSelect = false;
        public ListView MusicsListView;
        public Music VkSelectedAudio
        {
            get => _sad;
            set
            {
                _sad = value;
                PlayNewFile(value);
            }
        }
        public void ShowPlayedMusic()
        {
            if (Music.CurrentPlayingPlaylist == SelectedPlaylist && Music.CurrentPlayingMusic != null)
            {
                Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    AutoAudSelect = true;
                    MusicsListView.SelectedItem = null;
                    MusicsListView.SelectedItem = Music.CurrentPlayingMusic;
                });
            }
        }

        private Task PlayNewFile(Music val)
        {
            if (val != null)
            {
                if (AutoAudSelect)
                {
                    AutoAudSelect = false;
                    return null;
                }
                return Task.Run(() =>
                {
                    DurationGRDBiffered = 0;
                    DurationGRDCurrent = 0;
                    DurationLabelCurrent = "0:00";
                    DurationSliderCurrent = 0;
                    DurationSliderMaximum = 0.00001;

                    val.Play();

                    PlayButtonIsVisible = false;
                    PauseButtonIsVisible = true;
                });
            }
            else return null;
        }
        private void Play()
        {
            _MM.Play();
            PlayButtonIsVisible = false;
            PauseButtonIsVisible = true;
        }
        private void Pause()
        {
            _MM.Pause();
            PlayButtonIsVisible = true;
            PauseButtonIsVisible = false;
        }
        private void _MM_BufferedChanged(object sender, MediaManager.Playback.BufferedChangedEventArgs e)
        {
            DurationGRDBiffered = DurationGRDSize * (e.Buffered.TotalSeconds / _MM.Duration.TotalSeconds);
        }
        private void _MM_PositionChanged(object sender, MediaManager.Playback.PositionChangedEventArgs e)
        {
            DurationLabelCurrent = $"{e.Position:m\\:ss}";
            DurationGRDCurrent = DurationGRDSize * (e.Position.TotalSeconds / _MM.Duration.TotalSeconds);
            if (!DurationSliderLock) DurationSliderCurrent = e.Position.TotalSeconds;
        }
        private void _MM_StateChanged(object sender, MediaManager.Playback.StateChangedEventArgs e)
        {
            if (e.State == MediaManager.Player.MediaPlayerState.Playing)
                DurationSliderMaximum = _MM.Duration.TotalSeconds;
        }

        public bool DurationSliderLock = false;

        public double DurationGRDSize;
        public double DurationGRDBiffered { get; set; } = 0;
        public double DurationGRDCurrent { get; set; } = 0;
        public string DurationLabelCurrent { get; set; } = "0:00";
        public double DurationSliderCurrent { get; set; } = 0;
        public double DurationSliderMaximum { get; set; } = 0.00001;
    }

    public class Playlist
    {
        public Playlist(AudioPlaylist pl = null)
        {
            if (pl != null)
            {
                this.Id = pl.Id.Value;
                this.Count = Convert.ToInt32(pl.Count);
                this.Photo = pl.Photo.Photo600;
                this.Title = pl.Title;

                var audios = MainPageViewModel._vk.Audio.Get(new AudioGetParams
                {
                    OwnerId = MainPageViewModel._vk.UserId,
                    PlaylistId = this.Id,
                    Count = 6000
                });
                Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                while (Musics.Count() < Count)
                {
                    audios = MainPageViewModel._vk.Audio.Get(new AudioGetParams
                    {
                        OwnerId = MainPageViewModel._vk.UserId,
                        PlaylistId = this.Id,
                        Count = 6000
                    });
                    Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                }
            }
            else
            {
                this.Id = null;

                var audios = MainPageViewModel._vk.Audio.Get(new AudioGetParams
                {
                    OwnerId = MainPageViewModel._vk.UserId,
                    PlaylistId = this.Id,
                    Count = 6000
                });
                Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                int count = Musics.Count();
                while (count == 6000)
                {
                    audios = MainPageViewModel._vk.Audio.Get(new AudioGetParams
                    {
                        OwnerId = MainPageViewModel._vk.UserId,
                        PlaylistId = this.Id,
                        Count = 6000
                    });
                    Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                    count = audios.Count();
                }

                Count = Musics.Count();
            }
            AllPlaylists.Add(this);
        }

        public static readonly List<Playlist> AllPlaylists = new List<Playlist>();

        public int Count { get; private set; }
        public long? Id { get; private set; }
        public string Photo { get; private set; }
        public string Title { get; private set; }

        public List<Music> Musics { get; private set; }
        
        public List<Music> _Get(int skip, int take)
        {
            if (skip > Count)
                throw new Exception();
            else if (skip == 0)
            {
                if (take > Count) 
                    return Musics.Take(Count).ToList();
                else return Musics.Take(take).ToList();
            }
            else
            {
                if (skip + take > Count) 
                    return Musics.Skip(skip).Take(Count - skip).ToList();
                else return Musics.Skip(skip).Take(take).ToList();
            }
        }
    }

    public class Music
    {
        public Music(Audio a, long? PlaylistId, int MusicIndex)
        {
            this.Name = $"{a.Artist} - {a.Title}";
            this.Duration = $"{new TimeSpan(a.Duration * 10000000):m\\:ss}";

            this.PlaylistId = PlaylistId;
            this.MusicIndex = MusicIndex;
        }

        public string Name { get; private set; }
        public string Duration { get; private set; }
        public void Play()
        {
            if (CurrentPlayingMusic == null)
            {
                MainPageViewModel._MM.Play(this._GetUrl());
                CurrentPlayingMusic = this;
                CurrentPlayingPlaylist = Playlist.AllPlaylists.Where(p => p.Id == this.PlaylistId).First();
            }
            else
            {
                if (CurrentPlayingPlaylist.Id != CurrentPlayingMusic.PlaylistId)
                    CurrentPlayingPlaylist = Playlist.AllPlaylists.Where(p => p.Id == this.PlaylistId).First();
                MainPageViewModel._MM.Play(this._GetUrl());
                CurrentPlayingMusic = this;
            }
            SavePlayPositions();
        }
        public void PlayNext()
        {
            if (CurrentPlayingPlaylist.Musics.Last() == CurrentPlayingMusic)
            {
                CurrentPlayingMusic = CurrentPlayingPlaylist.Musics[0];
            }
            else
            {
                CurrentPlayingMusic = CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex + 1];
            }
            MainPageViewModel._MM.Play(CurrentPlayingMusic._GetUrl());
            SavePlayPositions();
        }
        public void PlayPrevious()
        {
            if (CurrentPlayingMusic.MusicIndex == 0)
            {
                CurrentPlayingMusic = CurrentPlayingPlaylist.Musics.Last();
            }
            else
            {
                CurrentPlayingMusic = CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex - 1];
            }
            MainPageViewModel._MM.Play(CurrentPlayingMusic._GetUrl());
            SavePlayPositions();
        }
        private static void SavePlayPositions()
        {
            if (CurrentPlayingMusic != null)
            {
                DM.VkPlayerPosition = new VkPlayerPosition
                {
                    PlaylistId = CurrentPlayingMusic.PlaylistId,
                    MusicIndex = CurrentPlayingMusic.MusicIndex
                };
            }
        }

        public static Music CurrentPlayingMusic { get; private set; }
        public static Playlist CurrentPlayingPlaylist { get; private set; }

        long? PlaylistId;
        int MusicIndex;
        public string _GetUrl()
        {
            return MainPageViewModel._vk.Audio.Get(new AudioGetParams
            {
                OwnerId = MainPageViewModel._vk.UserId,
                PlaylistId = this.PlaylistId,
                Offset = this.MusicIndex,
                Count = 1
            }).First().Url.ToString();
        }
    }
}