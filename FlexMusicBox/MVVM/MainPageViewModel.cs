using MediaManager;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        const int DownloadCount = 100;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        VkApi _vk;

        public Command Cmd_VkAuth { get; set; }
        public Command Cmd_VkShowAll { get; set; }
        public Command Cmd_VkSelectPlaylist { get; set; }
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
                    _ = VkOpened();
                });
            });
            Cmd_VkShowAll = new Command(() => 
            {
                Audios = null;
                SelectedPlaylist = null;
                Task.Run(() =>
                {
                    Audios = new ObservableCollection<Audio>(_vk.Audio.Get(new AudioGetParams 
                    { 
                        OwnerId = _vk.UserId, 
                        Count = DownloadCount 
                    }));
                });
            });
        }
        public Task FirstAppearing()
        {
            return Task.Run(() =>
            {
                var services = new ServiceCollection();
                services.AddAudioBypass();
                _vk = new VkApi(services);
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
        Task VkOpened()
        {
            return Task.Run(() => 
            {
                VkAuthGrdIsVisible = false;
                VkPlaylistGrdIsVisible = true;

                Playlists = new ObservableCollection<AudioPlaylist>(_vk.Audio.GetPlaylists(_vk.UserId.Value));
                Audios = new ObservableCollection<Audio>(_vk.Audio.Get(new AudioGetParams
                {
                    OwnerId = _vk.UserId,
                    Count = DownloadCount
                }));
            });
        }

        public async Task Scrolled(double height)
        {
            if (height >= Audios?.Count * 49)
            {
                AudioGetParams ap;
                if (SelectedPlaylist != null)
                {
                    ap = new AudioGetParams
                    {
                        OwnerId = _vk.UserId,
                        PlaylistId = SelectedPlaylist.Id,
                        Offset = Audios.Count,
                        Count = DownloadCount
                    };
                }
                else
                {
                    ap = new AudioGetParams
                    {
                        OwnerId = _vk.UserId,
                        Offset = Audios.Count,
                        Count = DownloadCount
                    };
                }
                foreach (var a in _vk.Audio.Get(ap))
                    Audios.Add(a);
                await Task.Yield();
                AudiosListHeight = Audios.Count * 49;
            }
        }

        public string VkLogin { get; set; }
        public string VkPass { get; set; }
        public string VkAuthInfo { get; set; }

        public bool VkAuthGrdIsVisible { get; set; } = false;
        public bool VkPlaylistGrdIsVisible { get; set; } = false;
        
        public ObservableCollection<AudioPlaylist> Playlists { get; set; }
        private Audio _sad;
        public Audio SelectedAudio
        {
            get => _sad;
            set
            {
                _sad = value;
                OnPropertyChanged();
                Task.Run(() =>
                {
                    CrossMediaManager.Current.Play(value.Url.ToString());

                    // PlayAudio +++++++++++++++++++++++++++++++++++++++++++++++
                });
            }
        }
        private AudioPlaylist _sap;
        public AudioPlaylist SelectedPlaylist
        {
            get => _sap;
            set
            {
                _sap = value;
                Audios = null;
                OnPropertyChanged();
                Task.Run(() =>
                {
                    Audios = new ObservableCollection<Audio>(_vk.Audio.Get(new AudioGetParams
                    {
                        OwnerId = _vk.UserId,
                        PlaylistId = value.Id,
                        Count = DownloadCount
                    }));
                });
            }
        }
        public double AudiosListHeight { get; set; }
        private ObservableCollection<Audio> _auds;
        public ObservableCollection<Audio> Audios
        {
            get => _auds;
            set
            {
                _auds = value;
                OnPropertyChanged();
                Task.Run(() =>
                {
                    AudiosListHeight = value.Count * 49;
                });
            }
        }



    }
}