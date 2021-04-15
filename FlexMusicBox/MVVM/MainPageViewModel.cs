using Android.Media;
using Android.Net;
using FlexMusicBox.Player;
using MediaManager;
using MediaManager.Library;
using MediaManager.Media;
using Microsoft.Extensions.DependencyInjection;
using OneWay.M3U;
using PlaylistsNET.Content;
using PlaylistsNET.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using VkNet;
using VkNet.Abstractions.Core;
using VkNet.AudioBypassService.Exceptions;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils.AntiCaptcha;
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

        public static IMediaManager _MM { get => CrossMediaManager.Current; }

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

        public class CaptchaHandler : ICaptchaHandler
        {
            public int MaxCaptchaRecognitionCount 
            { 
                get => 
                    5; 
                set => 
                    _ = value; }

            public T Perform<T>(Func<ulong?, string, T> action)
            {
                Thread.Sleep(5000);
                return action.Invoke(132, "dff");
                //throw new NotImplementedException();
            }
        }
        public class CaptchaSolver : ICaptchaSolver
        {
            public void CaptchaIsFalse()
            {
                var xx = this;

                throw new NotImplementedException();
            }

            public string Solve(string url)
            {
                var xx = this;

                throw new NotImplementedException();
            }
        }

        public MainPageViewModel()
        {
            Current = this;

            //_vk.CaptchaHandler = new CaptchaHandler();
            _vk.CaptchaSolver = new CaptchaSolver();

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
            _MM.MediaItemFinished += (s, e) =>
            {
                if (_MM.State != MediaManager.Player.MediaPlayerState.Playing)
                    Music.CurrentPlayingMusic?.PlayNext(true);
            };
                
        }

        public Task FirstAppearing()
        {
            //var zz = "https://archive.org/download/testmp3testfile/mpthreetest.mp3";

            //return Task.Run(() =>
            //{
            //    var mp = MediaPlayer.Create(Android.App.Application.Context, Android.Net.Uri.Parse(zz));

            //    mp.Start();
            //});






            //return null;

            

            //var player = new MediaPlayer();

            //player.BufferingUpdate += (s, e) =>
            //{
            //    Debug.WriteLine($" BufferingUpdate {e.Percent}");
            //};

            //player.SetDataSource(Android.App.Application.Context, Android.Net.Uri.Parse(zz));

            //player.Start();

            //return null;






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
            Task.Run(() => 
            {
                VkAuthGrdIsVisible = false;
                VkPlaylistGrdIsVisible = true;

                //Playlists = new ObservableCollection<Playlist>(_vk.Audio.GetPlaylists(_vk.UserId.Value).Select(p => new Playlist(p)));
                //DefaultPlaylist = new Playlist();
                //SelectedPlaylist = DefaultPlaylist;

                RestorePlaying();
            });
        }
        void RestorePlaying()
        {
            if (DM.Get_VkPlayerPosition(out var pp))
            {
                var xx = VM._vk.Audio.Get(new AudioGetParams
                {
                    OwnerId = VM._vk.UserId,
                    PlaylistId = pp.PlaylistId,
                    Offset = pp.MusicIndex,
                    Count = 1
                }).First().Url;


                var zz = xx.ToString();

                var fmp = new FlexMusicPlayer();

                fmp.Play(xx);


                //////////////////////////////////////////////////////////

                //var pls = Playlist.AllPlaylists.Where(p => p.Id == pp.PlaylistId).ToList();
                //if (pls.Count > 0)
                //{
                //    var msics = pls.First().Musics;
                //    if (msics.Count > pp.MusicIndex)
                //    {
                //        msics[pp.MusicIndex].Play();
                //        PlayerGrdIsVisible = true;
                //    }
                //}
            }
        }


        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
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
            var Nval = DurationGRDSize * (e.Buffered.TotalSeconds / _MM.Duration.TotalSeconds);
            if (double.IsNaN(Nval))
                DurationGRDBiffered = 0;
            else DurationGRDBiffered = Nval;
        }
        private void _MM_PositionChanged(object sender, MediaManager.Playback.PositionChangedEventArgs e)
        {
            DurationLabelCurrent = $"{e.Position:m\\:ss}";
            var Nval = DurationGRDSize * (e.Position.TotalSeconds / _MM.Duration.TotalSeconds);

            if (double.IsNaN(Nval))
                DurationGRDCurrent = 0;
            else DurationGRDCurrent = Nval;

            if (!DurationSliderLock) DurationSliderCurrent = e.Position.TotalSeconds;
        }
        private void _MM_StateChanged(object sender, MediaManager.Playback.StateChangedEventArgs e)
        {
            if (e.State == MediaManager.Player.MediaPlayerState.Playing)
                DurationSliderMaximum = _MM.Duration.TotalSeconds == 0 ? 0.00001 : _MM.Duration.TotalSeconds;
            if (e.State == MediaManager.Player.MediaPlayerState.Failed)
            {
                var x = _MM;
            }
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

    public class Music : NotifyPropertyChangedBase, IContentItem, INotifyPropertyChanged, IMediaItem
    {
        public event MetadataUpdatedEventHandler MetadataUpdated;

        public Music(Audio a, long? PlaylistId, int MusicIndex)
        {
            this._artist = a.Artist;
            this._title = a.Title;
            this._uri = a.Url;

            this._duration = TimeSpan.FromSeconds(a.Duration);

            this.PlaylistId = PlaylistId;
            this.MusicIndex = MusicIndex;
        }

        public System.Uri _uri;
        public string Name { get => $"{this.Artist} - {this.Title}"; }

        long? PlaylistId;
        int MusicIndex;

        private bool extracting = false;
        private bool extracted = false;
        private static readonly TimeSpan ExtractTimeout = TimeSpan.FromSeconds(30);
        private string _MediaUri;
        public string MediaUri
        {
            get
            {
                if (!extracted)
                {
                    if (extracting)
                    {
                        return _MediaUri = VM._vk.Audio.Get(new AudioGetParams
                        {
                            OwnerId = VM._vk.UserId,
                            PlaylistId = this.PlaylistId,
                            Offset = this.MusicIndex,
                            Count = 1
                        }).First().Url.ToString();
                    }
                    else
                    {
                        extracting = true;
                        var xx = VM._MM.Extractor.CreateMediaItem(this.MediaUri).GetAwaiter().GetResult();
                        VM._MM.Extractor.UpdateMediaItem(xx).GetAwaiter().GetResult();
                        this.FileName = xx.FileName;
                        this.FileExtension = xx.FileExtension;
                        this.MediaLocation = xx.MediaLocation;
                        this.MediaType = xx.MediaType;
                        this.DownloadStatus = xx.DownloadStatus;
                        this.IsMetadataExtracted = xx.IsMetadataExtracted;
                        extracting = false;
                        Task.Run(async () =>
                        {
                            extracted = true;
                            await Task.Delay(ExtractTimeout);
                            extracted = false;
                        });
                        return _MediaUri;
                    }
                }
                else return _MediaUri;
            }
            set => _ = value;
        }

        private string _id = Guid.NewGuid().ToString();
        public string Id
        {
            get 
            {
                var xx = _id;
                return _id;
            }
            set => SetProperty(ref _id, value);
        }

        public void Play()
        {
            if (CurrentPlayingMusic == null)
            {
                VM._MM.Play(this);
                CurrentPlayingMusic = this;
                CurrentPlayingPlaylist = Playlist.AllPlaylists.Where(p => p.Id == this.PlaylistId).First();
            }
            else
            {
                if (CurrentPlayingPlaylist.Id != this.PlaylistId)
                    CurrentPlayingPlaylist = Playlist.AllPlaylists.Where(p => p.Id == this.PlaylistId).First();
                VM._MM.Play(this);
                CurrentPlayingMusic = this;
            }
            VM.Current.ShowPlayedMusic();
            VM.Current.PlayerAudsInfo = new ObservableCollection<Music>
            {
                CurrentPlayingMusic.MusicIndex == 0
                ? CurrentPlayingPlaylist.Musics.Last()
                : CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex - 1],

                CurrentPlayingMusic,

                CurrentPlayingPlaylist.Musics.Last() == CurrentPlayingMusic
                ? CurrentPlayingPlaylist.Musics[0]
                : CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex + 1]
            };
            DM.VkPlayerPosition = new VkPlayerPosition
            {
                PlaylistId = this.PlaylistId,
                MusicIndex = this.MusicIndex
            };
        }
        public void PlayNext(bool auto = false)
        {
            if (CurrentPlayingPlaylist.Musics.Last() == CurrentPlayingMusic)
            {
                CurrentPlayingMusic = CurrentPlayingPlaylist.Musics[0];
            }
            else
            {
                CurrentPlayingMusic = CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex + 1];
            }
            VM.Current.PlayerAudsInfo.Add(CurrentPlayingPlaylist.Musics.Last() == CurrentPlayingMusic
                ? CurrentPlayingPlaylist.Musics[0]
                : CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex + 1]);
            VM.Current.PlayerAudsInfo.RemoveAt(0);

            if (auto)
            {
                VM._MM.Play(CurrentPlayingMusic);
                DM.VkPlayerPosition = new VkPlayerPosition
                {
                    PlaylistId = CurrentPlayingMusic.PlaylistId,
                    MusicIndex = CurrentPlayingMusic.MusicIndex
                };
            }
            else PlayNewFile();
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
            VM.Current.PlayerAudsInfo.Insert(0, CurrentPlayingMusic.MusicIndex == 0
                ? CurrentPlayingPlaylist.Musics.Last()
                : CurrentPlayingPlaylist.Musics[CurrentPlayingMusic.MusicIndex - 1]);
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
                _ = VM._MM.Play(CurrentPlayingMusic);
                VM.Current.ShowPlayedMusic();
                DM.VkPlayerPosition = new VkPlayerPosition
                {
                    PlaylistId = CurrentPlayingMusic.PlaylistId,
                    MusicIndex = CurrentPlayingMusic.MusicIndex
                };
            }
        }

        public static Music CurrentPlayingMusic { get; private set; }
        public static Playlist CurrentPlayingPlaylist { get; private set; }

        #region Interface
        private string _advertisement;
        public string Advertisement
        {
            get => _advertisement;
            set => SetProperty(ref _advertisement, value);
        }

        private string _album;
        public string Album
        {
            get => _album;
            set => SetProperty(ref _album, value);
        }

        private object _albumArt;
        public object AlbumImage
        {
            get => _albumArt;
            set => SetProperty(ref _albumArt, value);
        }

        private string _albumArtist;
        public string AlbumArtist
        {
            get => _albumArtist;
            set => SetProperty(ref _albumArtist, value);
        }

        private string _albumArtUri;
        public string AlbumImageUri
        {
            get => _albumArtUri;
            set => SetProperty(ref _albumArtUri, value);
        }

        private object _art;
        public object Image
        {
            get => _art;
            set => SetProperty(ref _art, value);
        }

        private string _artist;
        public string Artist
        {
            get => _artist;
            set => SetProperty(ref _artist, value);
        }

        private string _artUri;
        public string ImageUri
        {
            get => _artUri;
            set => SetProperty(ref _artUri, value);
        }

        private string _author;
        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        private string _compilation;
        public string Compilation
        {
            get => _compilation;
            set => SetProperty(ref _compilation, value);
        }

        private string _composer;
        public string Composer
        {
            get => _composer;
            set => SetProperty(ref _composer, value);
        }

        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private int _discNumber;
        public int DiscNumber
        {
            get => _discNumber;
            set => SetProperty(ref _discNumber, value);
        }

        private object _displayImage;
        public object DisplayImage
        {
            get
            {
                if (_displayImage != null)
                    return _displayImage;
                if (Image != null)
                    return Image;
                else if (AlbumImage != null)
                    return AlbumImage;
                else
                    return null;
            }

            set => SetProperty(ref _displayImage, value);
        }

        private string _displayImageUri;
        public string DisplayImageUri
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayImageUri))
                    return _displayImageUri;
                if (!string.IsNullOrEmpty(ImageUri))
                    return ImageUri;
                else if (!string.IsNullOrEmpty(AlbumImageUri))
                    return AlbumImageUri;
                else
                    return string.Empty;
            }

            set => SetProperty(ref _displayImageUri, value);
        }

        private string _displayTitle;
        public string DisplayTitle
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayTitle))
                    return _displayTitle;
                else if (!string.IsNullOrEmpty(Title))
                    return Title;
                else if (!string.IsNullOrEmpty(FileName))
                    return FileName;
                else
                    return string.Empty;
            }

            set => SetProperty(ref _displayTitle, value);
        }

        private string _displaySubtitle;
        public string DisplaySubtitle
        {
            get
            {
                if (!string.IsNullOrEmpty(_displaySubtitle))
                    return _displaySubtitle;
                else if (!string.IsNullOrEmpty(Artist))
                    return Artist;
                else if (!string.IsNullOrEmpty(AlbumArtist))
                    return AlbumArtist;
                else if (!string.IsNullOrEmpty(Album))
                    return Album;
                else
                    return string.Empty;
            }

            set => SetProperty(ref _displaySubtitle, value);
        }

        private string _displayDescription;
        public string DisplayDescription
        {
            get
            {
                if (!string.IsNullOrEmpty(_displayDescription))
                    return _displayDescription;
                else if (!string.IsNullOrEmpty(Album))
                    return Album;
                else if (!string.IsNullOrEmpty(Artist))
                    return Artist;
                else if (!string.IsNullOrEmpty(AlbumArtist))
                    return AlbumArtist;
                else
                    return string.Empty;
            }

            set => SetProperty(ref _displayDescription, value);
        }

        private DownloadStatus _downloadStatus = DownloadStatus.Unknown;
        public DownloadStatus DownloadStatus
        {
            get => _downloadStatus;
            set => SetProperty(ref _downloadStatus, value);
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        private object _extras;
        public object Extras
        {
            get => _extras;
            set => SetProperty(ref _extras, value);
        }

        private string _genre;
        public string Genre
        {
            get => _genre;
            set => SetProperty(ref _genre, value);
        }

        private int _numTracks;
        public int NumTracks
        {
            get => _numTracks;
            set => SetProperty(ref _numTracks, value);
        }

        private object _rating;
        public object Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private int _trackNumber;
        public int TrackNumber
        {
            get => _trackNumber;
            set => SetProperty(ref _trackNumber, value);
        }

        private object _userRating;
        public object UserRating
        {
            get => _userRating;
            set => SetProperty(ref _userRating, value);
        }

        private string _writer;
        public string Writer
        {
            get => _writer;
            set => SetProperty(ref _writer, value);
        }

        private int _year;
        public int Year
        {
            get => _year;
            set => SetProperty(ref _year, value);
        }

        private string _fileExtension;
        public string FileExtension
        {
            get => _fileExtension;
            set => SetProperty(ref _fileExtension, value);
        }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        private MediaType _mediaType = MediaType.Default;
        public MediaType MediaType
        {
            get => _mediaType;
            set => SetProperty(ref _mediaType, value);
        }

        private MediaLocation _mediaLocation = MediaLocation.Unknown;
        public MediaLocation MediaLocation
        {
            get => _mediaLocation;
            set => SetProperty(ref _mediaLocation, value);
        }

        private bool _isMetadataExtracted = false;
        public bool IsMetadataExtracted
        {
            get
            {
                return _isMetadataExtracted;
            }
            set
            {
                if (SetProperty(ref _isMetadataExtracted, value))
                    MetadataUpdated?.Invoke(this, new MetadataChangedEventArgs(this));
            }
        }

        private bool _isLive = false;
        public bool IsLive
        {
            get => _isLive;
            set => SetProperty(ref _isLive, value);
        }

#endregion
    }
}