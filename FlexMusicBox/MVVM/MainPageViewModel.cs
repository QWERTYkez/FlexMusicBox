using FlexMusicBox.Player;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
using VM = FlexMusicBox.MainPageViewModel;

namespace FlexMusicBox
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        const int MaxDispQuantity = 50;
        const int RowHeight = 48;

        public static VM Current;

        public static FlexMusicPlayer _MP;

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

        //public class CaptchaHandler : ICaptchaHandler
        //{
        //    public int MaxCaptchaRecognitionCount 
        //    { 
        //        get => 
        //            5; 
        //        set => 
        //            _ = value; }

        //    public T Perform<T>(Func<ulong?, string, T> action)
        //    {
        //        Thread.Sleep(5000);
        //        return action.Invoke(132, "dff");
        //        //throw new NotImplementedException();
        //    }
        //}
        //public class CaptchaSolver : ICaptchaSolver
        //{
        //    public void CaptchaIsFalse()
        //    {
        //        var xx = this;

        //        throw new NotImplementedException();
        //    }

        //    public string Solve(string url)
        //    {
        //        var xx = this;

        //        throw new NotImplementedException();
        //    }
        //}

        public Command Cmd_VkAuth { get; set; }
        public Command Cmd_VkShowAll { get; set; }
        public Command Cmd_VkSelectPlaylist { get; set; }
        public Command Cmd_Play { get; set; }
        public Command Cmd_Pause { get; set; }
        public Command Cmd_Next { get; set; }
        public Command Cmd_Previous { get; set; }
        public Command Cmd_ToAudioList { get; set; }
        public Command Cmd_ToPlayer { get; set; }
        public Command Cmd_Shuffle { get; set; }
        public Command Cmd_Repeat { get; set; }
        public Command Cmd_SwitchListToVK { get; set; }
        public Command Cmd_SwitchListToYandex { get; set; }
        public Command Cmd_SwitchPlayerToVK { get; set; }
        public Command Cmd_SwitchPlayerToYandex { get; set; }
        public Command Cmd_BackToList { get; set; }

        public MainPageViewModel()
        {
            Current = this;

            _MP = new FlexMusicPlayer();

            App.MediaNextPressed += () => Music.CurrentPlayingMusic?.PlayNext();
            App.MediaPreviousPressed += () => Music.CurrentPlayingMusic?.PlayPrevious();

            _vk.OnTokenExpires += s =>
            {
                _vk.RefreshToken();
                DM.VkToken = _vk.Token;
            };
            _vk.OnTokenUpdatedAutomatically += s => DM.VkToken = _vk.Token;
            //_vk.CaptchaHandler = new CaptchaHandler();
            //_vk.CaptchaSolver = new CaptchaSolver();

            Cmd_SwitchListToVK = new Command(() =>
            {
                if (VkAuthorized)
                {

                }
                else
                {
                    VkAuthGrdIsVisible = true;
                    PlaylistGrdIsVisible = false;
                }
            });
            Cmd_SwitchListToYandex = new Command(() =>
            {
                if (YaAuthorized)
                {

                }
                else
                {
                    YaAuthGrdIsVisible = true;
                    PlaylistGrdIsVisible = false;
                }
            });
            Cmd_BackToList = new Command(() =>
            {
                PlaylistGrdIsVisible = true;
                VkAuthGrdIsVisible = false;
                YaAuthGrdIsVisible = false;
            });
            Cmd_SwitchPlayerToVK = new Command(() =>
            {
                //if (YaAuthorized)
                //{

                //}
                //else
                //{
                //    VkAuthGrdIsVisible = true;
                //    PlaylistGrdIsVisible = false;
                //}
            });
            Cmd_SwitchPlayerToYandex = new Command(() =>
            {
                //if (YaAuthorized)
                //{

                //}
                //else
                //{
                //    VkAuthGrdIsVisible = true;
                //    PlayerGrdIsVisible = false;
                //}
            });

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

                    DM.SaveProps(new VkProps
                    {
                        VkUserAuth = new VkUserAuth(Login, Pass, _vk.UserId.Value),
                        VkToken = _vk.Token
                    });

                    VkAuthInfo = "Авторизация пройдена";

                    await Task.Delay(1000);
                    Await1("Загрузка плейлистов");
                    OnVkAuthorized();
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
                PlaylistGrdIsVisible = true;
                PlayerGrdIsVisible = false;
            });
            Cmd_ToPlayer = new Command(() =>
            {
                PlaylistGrdIsVisible = false;
                PlayerGrdIsVisible = true;
            });
            Cmd_Shuffle = new Command(() => ShuffleMode = !ShuffleMode);
            Cmd_Repeat = new Command(() => RepeatMode = !RepeatMode);

            _MP.Buffered += p => DurationGRDBiffered = DurationGRDSize * p;
            _MP.DurationAccepted += ts => DurationSliderMaximum = ts.TotalSeconds;
            _MP.PositionChanged += position =>
            {
                DurationLabelCurrent = $"{position:m\\:ss}";
                var Nval = DurationGRDSize * (position.TotalSeconds / DurationSliderMaximum);

                if (double.IsNaN(Nval))
                    DurationGRDCurrent = 0;
                else DurationGRDCurrent = Nval;

                if (!DurationSliderLock) DurationSliderCurrent = position.TotalSeconds;
            };
            _MP.Finished += () =>
            {
                if (RepeatMode)
                {
                    _MP.SeekTo(0);
                }
                else Music.CurrentPlayingMusic?.PlayNext(true);
            };
        }

        public Task FirstAppearing()
        {
            return Task.Run(() =>
            {
                if (DM.Get_SourceType(out var st))
                {
                    st = SourceType.Vkontakte;
                    switch (st)
                    {
                        case SourceType.Vkontakte:
                            {
                                if (DM.Get_VkPlayerPosition(out VkPlayerPosition pp))
                                    Await2("Авторизация ВК");
                                else Await1("Авторизация ВК");

                                VkAuthorization(pp);
                            }
                            break;
                        case SourceType.Yandex:
                            {

                            }
                            break;
                        case SourceType.None: goto SourceTypeFalse;
                    }
                }
                else goto SourceTypeFalse;
        SourceTypeFalse:
                {
                    PlaylistGrdIsVisible = true;
                    Await1("Авторизация ВК");
                    VkAuthorization();
                }
            });
        }
        void VkAuthorization(VkPlayerPosition pp = null)
        {
            ApiAuthParams Ap = null;
            if (DM.Get_VkUserAuth(out var UserAuth))
            {
                Ap = UserAuth.ApiAuthParams;
                VkLogin = UserAuth.Login;
                VkPass = UserAuth.Pass;

                if (DM.Get_VkToken(out var token))
                {
                    try
                    {
                        _vk.Authorize(new ApiAuthParams { AccessToken = token, UserId = Ap.UserId });
                        OnVkAuthorized(pp);
                        return;
                    }
                    catch
                    {
                        if (Ap != null)
                        {
                            _vk.Authorize(Ap);
                            DM.VkToken = _vk.Token;
                            OnVkAuthorized(pp);
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
                        OnVkAuthorized(pp);
                        return;
                    }
                }
            }
            Awaited();
        }
        void OnVkAuthorized(VkPlayerPosition pp = null)
        {
            AwaitMessage = "Загрузка плейлистов ВК";
            VkAuthorized = true;

            Playlists = new ObservableCollection<Playlist>(_vk.Audio.GetPlaylists(_vk.UserId.Value).Select(p => new Playlist(p)));
            DefaultPlaylist = new Playlist();
            SelectedPlaylist = DefaultPlaylist;

            if (pp != null)
            {
                AwaitMessage = "Восстановление проигрывателя";
                var pls = Playlist.AllPlaylists.Where(p => p.Id == pp.PlaylistId).ToList();
                if (pls.Count > 0)
                {
                    var msics = pls.First().Musics;
                    if (msics.Count > pp.MusicIndex)
                    {
                        __shuffleMode = pp.Shuffle;
                        msics[pp.MusicIndex].Play();
                    }
                    else PlaylistGrdIsVisible = true;
                }
                else PlaylistGrdIsVisible = true;
            }

            Awaited();
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

        public string AwaitMessage { get; set; }
        public bool Awaiting { get; set; } = false;
        private void Awaited() => Awaiting = false;

        public Func<Task<bool>> Animation1;
        private void Await1(string msg)
        {
            AwaitMessage = msg;
            Awaiting = true;
            PlaylistGrdIsVisible = true;
            Task.Run(async () =>
            {
                while (Awaiting)
                    await Animation1?.Invoke();
            });
        }
        public Func<Task<bool>> Animation2;
        private void Await2(string msg)
        {
            AwaitMessage = msg;
            Awaiting = true;
            PlayerGrdIsVisible = true;
            Task.Run(async () =>
            {
                while (Awaiting)
                    await Animation2?.Invoke();
            });
        }

        public string VkLogin { get; set; }
        public string VkPass { get; set; }
        public string VkAuthInfo { get; set; }

        public bool VkAuthorized { get; set; } = false;
        public bool YaAuthorized { get; set; } = false;

        public bool VkAuthGrdIsVisible { get; set; } = false;
        public bool YaAuthGrdIsVisible { get; set; } = false;
        public bool PlaylistGrdIsVisible { get; set; } = false;
        public bool PlayerGrdIsVisible { get; set; } = false;      ////////////

        public SourceType _currentSource = SourceType.None;
        public SourceType CurrentSource { get; set; }

        public bool PlayButtonIsVisible { get; set; } = false;
        public bool PauseButtonIsVisible { get; set; } = true;

        public bool ShuffleMode { get => _ShuffleMode; set => _ShuffleMode = value; }
        private static bool __shuffleMode = false;
        public static bool _ShuffleMode 
        {
            get => __shuffleMode;
            set
            {
                if (__shuffleMode != value)
                    Music.Shuffle(value);
                __shuffleMode = value;
            }
        }

        public bool RepeatMode { get => _RepeatMode; set => _RepeatMode = value; }
        public static bool _RepeatMode { get; set; } = false;

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
                Task.Run(() =>
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

        public ObservableCollection<Music> PlayerAudsInfo { get; set; }

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
            _MP.Start();
            PlayButtonIsVisible = false;
            PauseButtonIsVisible = true;
        }
        private void Pause()
        {
            _MP.Pause();
            PlayButtonIsVisible = true;
            PauseButtonIsVisible = false;
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

                var audios = VM._vk.Audio.Get(new AudioGetParams
                {
                    OwnerId = VM._vk.UserId,
                    PlaylistId = this.Id,
                    Count = 6000
                });
                Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                while (Musics.Count() < Count)
                {
                    audios = VM._vk.Audio.Get(new AudioGetParams
                    {
                        OwnerId = VM._vk.UserId,
                        PlaylistId = this.Id,
                        Count = 6000
                    });
                    Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                }
            }
            else
            {
                this.Id = null;

                var audios = VM._vk.Audio.Get(new AudioGetParams
                {
                    OwnerId = VM._vk.UserId,
                    PlaylistId = this.Id,
                    Count = 6000
                });
                Musics = audios.Select(a => new Music(a, this.Id, audios.IndexOf(a))).ToList();
                int count = Musics.Count();
                while (count == 6000)
                {
                    audios = VM._vk.Audio.Get(new AudioGetParams
                    {
                        OwnerId = VM._vk.UserId,
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
            this.Artist = a.Artist;
            this.Title = a.Title;

            this.Duration = TimeSpan.FromSeconds(a.Duration);

            this.PlaylistId = PlaylistId;
            this.MusicIndex = MusicIndex;
        }

        public string Name { get => $"{this.Artist} - {this.Title}"; }

        public string Artist;
        public string Title;
        public TimeSpan Duration;

        long? PlaylistId;
        int MusicIndex;

        public Uri _GetUri()
        {
            return VM._vk.Audio.Get(new AudioGetParams
            {
                OwnerId = VM._vk.UserId,
                PlaylistId = this.PlaylistId,
                Offset = this.MusicIndex,
                Count = 1
            }).First().Url;
        }

        public void Play()
        {
            CurrentPlayingMusic = this;
            CurrentPlayingPlaylist = Playlist.AllPlaylists.
                    Where(p => p.Id == this.PlaylistId).First();

            Shuffle(VM._ShuffleMode);

            VM.Current.ShowPlayedMusic();

            VM._MP.PlayNew(this);

            DM.VkPlayerPosition = new VkPlayerPosition
            {
                PlaylistId = CurrentPlayingMusic.PlaylistId,
                MusicIndex = CurrentPlayingMusic.MusicIndex,
                Shuffle = VM._ShuffleMode
            };
        }
        public void PlayNext(bool auto = false)
        {
            if (CurrentPlayingCollection.Last() == CurrentPlayingMusic)
            {
                CurrentPlayingMusic = CurrentPlayingCollection[0];
            }
            else
            {
                CurrentPlayingMusic = CurrentPlayingCollection[CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) + 1];
            }
            VM.Current.PlayerAudsInfo.Add(CurrentPlayingCollection.Last() == CurrentPlayingMusic
                ? CurrentPlayingCollection[0]
                : CurrentPlayingCollection[CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) + 1]);
            VM.Current.PlayerAudsInfo.RemoveAt(0);

            if (auto)
            {
                VM._MP.PlayNew(CurrentPlayingMusic);
                VM.Current.ShowPlayedMusic();
                DM.VkPlayerPosition = new VkPlayerPosition
                {
                    PlaylistId = CurrentPlayingMusic.PlaylistId,
                    MusicIndex = CurrentPlayingMusic.MusicIndex,
                    Shuffle = VM._ShuffleMode
                };
            }
            else PlayNewFile();
        }
        public void PlayPrevious()
        {
            if (CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) == 0)
            {
                CurrentPlayingMusic = CurrentPlayingCollection.Last();
            }
            else
            {
                CurrentPlayingMusic = CurrentPlayingCollection[CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) - 1];
            }
            VM.Current.PlayerAudsInfo.Insert(0, CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) == 0
                ? CurrentPlayingCollection.Last()
                : CurrentPlayingCollection[CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) - 1]);
            VM.Current.PlayerAudsInfo.RemoveAt(3);

            PlayNewFile();
        }

        private const int delay = 1000;
        private static int counter = 0;
        private static async void PlayNewFile()
        {
            counter += 1;
            await Task.Delay(delay);
            counter -= 1;
            if (counter == 0)
            {
                VM._MP.PlayNew(CurrentPlayingMusic);
                VM.Current.ShowPlayedMusic();
                DM.VkPlayerPosition = new VkPlayerPosition
                {
                    PlaylistId = CurrentPlayingMusic.PlaylistId,
                    MusicIndex = CurrentPlayingMusic.MusicIndex,
                    Shuffle = VM._ShuffleMode
                };
            }
        }

        public static void Shuffle(bool Suffle)
        {
            if (Suffle)
            {
                CurrentPlayingCollection = new List<Music>(CurrentPlayingPlaylist.Musics);

                var r = new Random();
                int n = CurrentPlayingCollection.Count;
                Music buf;
                while (n > 1)
                {
                    n--;
                    int k = r.Next(n + 1);
                    buf = CurrentPlayingCollection[k];
                    CurrentPlayingCollection[k] = CurrentPlayingCollection[n];
                    CurrentPlayingCollection[n] = buf;
                }
            }
            else
            {
                CurrentPlayingCollection = new List<Music>(CurrentPlayingPlaylist.Musics);
            }

            VM.Current.PlayerAudsInfo = new ObservableCollection<Music>
            {
                CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) == 0
                ? CurrentPlayingCollection.Last()
                : CurrentPlayingCollection[CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) - 1],

                CurrentPlayingMusic,

                CurrentPlayingCollection.Last() == CurrentPlayingMusic
                ? CurrentPlayingCollection[0]
                : CurrentPlayingCollection[CurrentPlayingCollection.IndexOf(CurrentPlayingMusic) + 1]
            };

            DM.VkPlayerPosition = new VkPlayerPosition
            {
                PlaylistId = CurrentPlayingMusic.PlaylistId,
                MusicIndex = CurrentPlayingMusic.MusicIndex,
                Shuffle = VM._ShuffleMode
            };
        }

        public static Music CurrentPlayingMusic { get; private set; }
        public static Playlist CurrentPlayingPlaylist { get; private set; }
        private static List<Music> CurrentPlayingCollection = new List<Music>();
    }

    public enum SourceType
    {
        None,
        Vkontakte,
        Yandex
    }
}