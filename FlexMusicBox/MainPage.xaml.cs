using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VkNet;
using VkNet.AudioBypassService.Exceptions;
using VkNet.AudioBypassService.Extensions;
using VkNet.Model;
using Xamarin.Forms;

namespace FlexMusicBox
{
    public partial class MainPage : ContentPage
    {
        VkApi _vk;
        private Task SaveProp(string Name, string Prop)
        {
            App.Current.Properties[Name] = Prop;
            return App.Current.SavePropertiesAsync();
        }
        private Task SaveProp<T>(string Name, T Prop) where T: class
        {
            App.Current.Properties[Name] = JsonConvert.SerializeObject(Prop);
            return App.Current.SavePropertiesAsync();
        }
        private Task SaveProps(Dictionary<string, object> Props)
        {
            foreach (var x in Props)
            {
                if (x.Value is string) App.Current.Properties[x.Key] = x.Value;
                else
                {
                    var T = x.Value.GetType();
                    if (T.IsClass)
                        App.Current.Properties[x.Key] =
                            JsonConvert.SerializeObject(Convert.ChangeType(x.Value, T));
                    else throw new Exception("Неправильное использование App.Current.Properties");
                }
            }
            return App.Current.SavePropertiesAsync();
        }
        private bool GetProp(string Name, out string Prop)
        {
            if (App.Current.Properties.TryGetValue(Name, out object o))
            {
                Prop = o as string;
                return true;
            }
            else
            {
                Prop = null;
                return false;
            }
        }
        private bool GetProp<T>(string Name, out T Prop) where T : class
        {
            if (App.Current.Properties.TryGetValue(Name, out object o))
            {
                Prop = JsonConvert.DeserializeObject<T>(o as string);
                return true;
            }
            else
            {
                Prop = null;
                return false;
            }
        }

        public MainPage()
        {
            InitializeComponent();

            this.Appearing += FirstAppearing;
        }

        private void FirstAppearing(object sender, EventArgs e)
        {
            this.Appearing -= FirstAppearing;

            Task.Run(() =>
            {
                var services = new ServiceCollection();
                services.AddAudioBypass();
                _vk = new VkApi(services);
                _vk.OnTokenExpires += s =>
                {
                    _vk.RefreshToken();
                    SaveProp("VkToken", _vk.Token);
                };
                _vk.OnTokenUpdatedAutomatically += s => SaveProp("VkToken", _vk.Token);

                ApiAuthParams Ap = null;
                if (GetProp("VkUserAuth", out VkUserAuth UserAuth))
                {
                    Ap = UserAuth.ApiAuthParams;

                    Dispatcher.BeginInvokeOnMainThread(() =>
                    {
                        VkLogin.Text = UserAuth.Login;
                        VkPass.Text = UserAuth.Pass;
                    });
                }
                if (GetProp("VkToken", out string token))
                {
                    try
                    {
                        _vk.Authorize(new ApiAuthParams { AccessToken = token });
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
                            SaveProp("VkToken", _vk.Token);
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
                        SaveProp("VkToken", _vk.Token);
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

        private void GoToAuth(object sender, EventArgs e)
        {
            VkontacteAuthGRD.IsVisible = true;
            PlayerGRD.IsVisible = false;
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
                    if(GetProp("VkToken", out string token))
                        _vk.Authorize(new ApiAuthParams { AccessToken = token });
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

                _ = SaveProps(new Dictionary<string, object>
                {
                    {"VkUserAuth", new VkUserAuth(Login, Pass) },
                    {"VkToken", _vk.Token }
                });

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