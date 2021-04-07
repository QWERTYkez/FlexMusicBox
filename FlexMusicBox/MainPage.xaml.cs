using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using VkNet;
using VkNet.AudioBypassService.Exceptions;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using Xamarin.Essentials;
using Xamarin.Forms;
using Newtonsoft.Json;

namespace FlexMusicBox
{
    public partial class MainPage : ContentPage
    {
        VkApi _vk;
        private IDictionary<string, object> Prop { get => App.Current.Properties; }
        private Task SaveProps() => App.Current.SavePropertiesAsync();

        public MainPage()
        {
            InitializeComponent();

            this.Appearing += (s, e) => Task.Run(() =>
            {
                var services = new ServiceCollection();
                services.AddAudioBypass();
                _vk = new VkApi(services);
                _vk.OnTokenExpires += s =>
                {
                    _vk.RefreshToken();
                    Prop["VkToken"] = _vk.Token;
                    SaveProps();
                };

                ApiAuthParams Ap = null;
                if (Prop.TryGetValue("VkUserAuth", out object o))
                {
                    var UserAuth = JsonConvert.DeserializeObject<VkUserAuth>(o as string);
                    Ap = UserAuth.ApiAuthParams;

                    Dispatcher.BeginInvokeOnMainThread(() =>
                    {
                        VkLogin.Text = UserAuth.Login;
                        VkPass.Text = UserAuth.Pass;
                    });
                }
                if (Prop.TryGetValue("VkToken", out object Token))
                {
                    try
                    {
                        _vk.Authorize(new ApiAuthParams { AccessToken = (string)Token });
                        Dispatcher.BeginInvokeOnMainThread(() =>
                        {
                            VkontacteAuthGRD.IsVisible = false;
                            PlayerGRD.IsVisible = true;
                        });
                        return;
                    }
                    catch
                    {
                        if (Ap != null)
                        {
                            _vk.Authorize(Ap);
                            Prop["VkToken"] = _vk.Token;
                            SaveProps();
                            Dispatcher.BeginInvokeOnMainThread(() =>
                            {
                                VkontacteAuthGRD.IsVisible = false;
                                PlayerGRD.IsVisible = true;
                            });
                            return;
                        }
                    }
                }
                else
                {
                    if (Ap != null)
                    {
                        _vk.Authorize(Ap);
                        Prop["VkToken"] = _vk.Token;
                        SaveProps();
                        Dispatcher.BeginInvokeOnMainThread(() =>
                        {
                            VkontacteAuthGRD.IsVisible = false;
                            PlayerGRD.IsVisible = true;
                        });
                        return;
                    }
                }
                Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    VkontacteAuthGRD.IsVisible = true;
                });
            });
        }

        private void VkAuthPressed(object sender, EventArgs e)
        {
            VkAuthInfo.Text = "Попытка входа...";
            var Login = VkLogin.Text;
            var Pass = VkPass.Text;
            Task.Run(async () =>
            {
                try
                {
                    if (Prop.TryGetValue("VkToken", out object Token))
                        _vk.Authorize(new ApiAuthParams { AccessToken = (string)Token });
                    else
                    {
                        _vk.Authorize(new ApiAuthParams
                        {
                            Login = Login,
                            Password = Pass
                        });
                    }
                }
                catch (VkAuthException ex)
                {
                    Dispatcher.BeginInvokeOnMainThread(() =>
                    {
                        VkAuthInfo.Text = ex.Message;
                    });
                    return;
                }

                Prop["VkUserAuth"] = JsonConvert.SerializeObject(new VkUserAuth(Login, Pass));
                Prop["VkToken"] = _vk.Token;
                _ = SaveProps();

                Dispatcher.BeginInvokeOnMainThread(() => VkAuthInfo.Text = "Авторизация пройдена");

                await Task.Delay(1000);
                Dispatcher.BeginInvokeOnMainThread(() =>
                {
                    VkontacteAuthGRD.IsVisible = false;
                    PlayerGRD.IsVisible = true;
                });
            });
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class VkUserAuth
        {
            public VkUserAuth() { }
            public VkUserAuth(string Login, string Pass)
            {
                this.Login = Login; this.Pass = Pass;
            }

            [JsonProperty]
            public string Login { get; set; }
            [JsonProperty]
            public string Pass { get; set; }

            public ApiAuthParams ApiAuthParams { get => new ApiAuthParams { Login = this.Login, Password = this.Pass }; }
        }
    }
}

//var audios = vk.Audio.Get(new AudioGetParams { OwnerId = vk.UserId, Count = 10 });

//Task.Run(async () =>
//{
//    await CrossMediaManager.Current.Play(audios[0].Url.ToString());
//});