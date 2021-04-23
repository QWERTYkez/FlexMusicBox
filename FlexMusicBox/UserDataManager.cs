using Newtonsoft.Json;
using System;
using VkNet.Model;

namespace FlexMusicBox
{
    public static class UserDataManager
    {
        public static bool Get_VkToken(out string obj) => GetProp("VkToken", out obj);
        public static string VkToken { set => SaveProp("VkToken", value); }

        public static bool Get_VkUserAuth(out VkUserAuth obj) => GetProp("VkUserAuth", out obj);
        public static VkUserAuth VkUserAuth { set => SaveProp("VkUserAuth", value); }

        public static bool Get_YaUserAuth(out YaUserAuth obj) => GetProp("YaUserAuth", out obj);
        public static YaUserAuth YaUserAuth { set => SaveProp("YaUserAuth", value); }

        public static bool Get_VkPlayerPosition(out VkPlayerPosition obj)
        {
            
            if (GetProp("VkPlayerPosition", out obj))
            {
                if (Get_VkShiftMS(out int sec))
                {
                    obj.ShiftMS = sec;
                }
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }
            
        public static VkPlayerPosition VkPlayerPosition { set => SaveProp("VkPlayerPosition", value); }

        public static bool Get_YaPlayerPosition(out YaPlayerPosition obj)
        {
            if (GetProp("YaPlayerPosition", out obj))
            {
                if (Get_YaShiftMS(out int sec))
                {
                    obj.ShiftMS = sec;
                }
                return true;
            }
            else
            {
                obj = null;
                return false;
            }
        }
        public static YaPlayerPosition YaPlayerPosition { set => SaveProp("YaPlayerPosition", value); }

        public static bool Get_VkShiftMS(out int obj) => GetProp("VkShiftMS", out obj);
        public static int VkShiftMS { set => SaveProp("VkShiftMS", value); }

        public static bool Get_YaShiftMS(out int obj) => GetProp("YaShiftMS", out obj);
        public static int YaShiftMS { set => SaveProp("YaShiftMS", value); }

        public static bool Get_SourceType(out SourceType obj)
        {
            var b = GetProp("SourceType", out int i);
            obj = i switch
            {
                0 => SourceType.None,
                1 => SourceType.Vkontakte,
                2 => SourceType.Yandex,
                _ => throw new Exception("такого источника не бывает")
            };
            return b;
        }
        public static SourceType SourceType 
        { 
            set 
            {
                int i = value switch
                {
                    SourceType.None => 0,
                    SourceType.Vkontakte => 1,
                    SourceType.Yandex => 2,
                    _ => throw new Exception("такого источника не бывает")
                };
                SaveProp("SourceType", i);
            } 
        }

        public static void SaveProps(VkProps props)
        {
            if (props.VkToken != null)
            {
                App.Current.Properties["VkToken"] = props.VkToken;
            }
            if (props.VkUserAuth != null)
            {
                App.Current.Properties["VkUserAuth"] = JsonConvert.SerializeObject(props.VkUserAuth);
            }
            App.Current.SavePropertiesAsync();
        }

        private static void SaveProp(string Name, string Prop)
        {
            App.Current.Properties[Name] = Prop;
            App.Current.SavePropertiesAsync();
        }
        private static void SaveProp(string Name, int Prop)
        {
            App.Current.Properties[Name] = Prop.ToString();
            App.Current.SavePropertiesAsync();
        }
        private static void SaveProp<T>(string Name, T Prop) where T : class
        {
            App.Current.Properties[Name] = JsonConvert.SerializeObject(Prop);
            App.Current.SavePropertiesAsync();
        }
        private static bool GetProp(string Name, out string Prop)
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
        private static bool GetProp(string Name, out int Prop)
        {
            if (App.Current.Properties.TryGetValue(Name, out object o))
            {
                Prop = Convert.ToInt32(o);
                return true;
            }
            else
            {
                Prop = 0;
                return false;
            }
        }
        private static bool GetProp<T>(string Name, out T Prop) where T: class
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
    }

    public class VkProps
    {
        public string VkToken { get; set; } = null;
        public VkUserAuth VkUserAuth { get; set; } = null;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class VkPlayerPosition
    {
        [JsonProperty]
        public long? PlaylistId { get; set; }
        [JsonProperty]
        public int MusicIndex { get; set; }
        [JsonProperty]
        public bool Shuffle { get; set; }
        public int ShiftMS { get; set; } = -1;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class YaPlayerPosition
    {
        [JsonProperty]
        public string OwnerName { get; set; }
        [JsonProperty]
        public string Kind { get; set; }
        [JsonProperty]
        public string Id { get; set; }
        [JsonProperty]
        public bool Shuffle { get; set; }
        public int ShiftMS { get; set; } = -1;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class VkUserAuth
    {
        public VkUserAuth() { }
        public VkUserAuth(string Login, string Pass, long Id)
        {
            this.Login = Login; this.Pass = Pass; this.Id = Id;
        }

        [JsonProperty]
        public string Login { get; set; }
        [JsonProperty]
        public string Pass { get; set; }
        [JsonProperty]
        public long Id { get; set; }

        public ApiAuthParams ApiAuthParams { get => new ApiAuthParams { Login = this.Login, Password = this.Pass, UserId = this.Id }; }
    }

    public class YaUserAuth
    {
        public YaUserAuth() { }
        public YaUserAuth(string Login, string Pass, string Token)
        {
            this.Login = Login; this.Pass = Pass; this.Token = Token;
        }

        public string Login { get; set; }
        public string Pass { get; set; }
        public string Token { get; set; }
    }
}
