using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text.Encodings.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FistirDictionary
{
    public class DictionaryEntry
    {
        public string? DictionaryPath { get; set; }
        public string? ScansionScript { get; set; }
        public string? DerivationScript { get; set; }
    }

    internal class DictionaryEntryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DictionaryEntry);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string dictionaryPath = jo["DictionaryPath"].Value<string>();
            string scansionScript = jo["ScansionScript"].Value<string>();
            string derivationScript = jo["DerivationScript"].Value<string>();
            return new DictionaryEntry
            {
                DictionaryPath = dictionaryPath,
                ScansionScript = scansionScript,
                DerivationScript = derivationScript
            };
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var dictionaryEntry = (DictionaryEntry)value;
            var jo = new JObject();
            jo.Add("DictionaryPath", dictionaryEntry.DictionaryPath);
            jo.Add("ScansionScript", dictionaryEntry.ScansionScript);
            jo.Add("DerivationScript", dictionaryEntry.DerivationScript);
            jo.WriteTo(writer);
        }
    }

    internal class DictionaryGroup
    {
        public string? GroupName { get; set;}
        public List<DictionaryEntry>? DictionaryEntries { get; set;}
        public int? DefaultDictionaryIndex { get; set;}
    }

    internal class DictionaryGroupConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DictionaryGroup);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string groupName = jo["GroupName"].Value<string>();
            int defaultDictionaryIndex = jo["DefaultDictionaryIndex"].Value<int>();

            var dictionaryEntryArray = jo["DictionaryEntries"].ToObject<JArray>();
            var dictionaryEntries = new List<DictionaryEntry>();
            foreach (var de in dictionaryEntryArray)
            {
                var dictionaryGroup = de.ToObject<DictionaryEntry>();
                dictionaryEntries.Add(dictionaryGroup);
            }

            return new DictionaryGroup { DictionaryEntries = dictionaryEntries, DefaultDictionaryIndex = defaultDictionaryIndex, GroupName = groupName };
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var dictionaryGroup = (DictionaryGroup)value;
            var jo = new JObject();
            jo.Add("GroupName", dictionaryGroup.GroupName);
            jo.Add("DefaultDictionaryIndex", dictionaryGroup.DefaultDictionaryIndex);
            jo.Add("DictionaryEntries", JArray.FromObject(dictionaryGroup.DictionaryEntries));
            jo.WriteTo(writer);
        }
    }

    internal class Settings
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public List<DictionaryGroup>? DictionaryGroups { get; set; }
        public int? DefaultGroupIndex { get; set; }

        public void Save(string settingsPath)
        {
            using (var fs = new System.IO.FileStream(settingsPath, System.IO.FileMode.Open, System.IO.FileAccess.Write))
            {
                fs.SetLength(0);
                using (var sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("UTF-8")))
                {
                    var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                    sw.Write(json);
                }
            }
        }
    }

    internal class SettingsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Settings));
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string? username = (string?)jo["Username"];
            string? email = (string?)jo["Email"];
            int? defaultGroupIndex = (int?)jo["DefaultGroupIndex"];

            var dictionaryGroupArray = jo["DictionaryGroups"].ToObject<JArray>();
            var dictionaryGroups = new List<DictionaryGroup>();
            foreach (var dg in dictionaryGroupArray)
            {
                var dictionaryGroup = dg.ToObject<DictionaryGroup>();
                dictionaryGroups.Add(dictionaryGroup);
            }

            return new Settings
            {
                Username = username,
                Email = email,
                DefaultGroupIndex = defaultGroupIndex,
                DictionaryGroups = dictionaryGroups
            };
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var settings = (Settings)value;
            var jo = new JObject();
            jo.Add("Username", settings.Username);
            jo.Add("Email", settings.Email);
            jo.Add("DefaultGroupIndex", settings.DefaultGroupIndex);
            jo.Add("DictionaryGroups", JArray.FromObject(settings.DictionaryGroups));
            jo.WriteTo(writer);
        }
    }

    internal class SettingSerializer
    {
        /// <summary>
        ///   JSON設定ファイルをロードし、Settingsオブジェクトを取得します。
        ///   設定ファイルが存在しない場合、デフォルトの設定ファイルを生成します。
        ///   デフォルトの設定ファイル内のサンプル辞書を自動的に作成します。
        /// </summary>
        /// <param name="settingsPath"></param>
        /// <returns></returns>
        public static Settings LoadSettings(string settingsPath)
        {
            Settings settings;
            string json;
            if (!File.Exists(settingsPath))
            {
                // 設定ファイルを新規設定
                var homeDir = Path.GetDirectoryName(path: System.Reflection.Assembly.GetEntryAssembly().Location);
                var sampleDictPath = Path.Combine(homeDir, "サンプル.fdic");
                if (!File.Exists(sampleDictPath))
                {
                    FDictionary.CreateEmptyDictionary(sampleDictPath, "サンプル", "サンプル辞書です。", "", "", true, "Kapahata <charzkpht@gmail.com>");
                }
                var dictionaryGroup = new DictionaryGroup { GroupName = "サンプル", DictionaryEntries = new List<DictionaryEntry>() { 
                    new DictionaryEntry { DictionaryPath = sampleDictPath }
                } };
                var dgs = new List<DictionaryGroup> { dictionaryGroup };
                settings = new Settings { DictionaryGroups = dgs, Username = null, Email = null };
                using (var fs = new System.IO.FileStream(settingsPath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                {
                    using (var sw = new StreamWriter(fs, System.Text.Encoding.GetEncoding("UTF-8")))
                    {
                        json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                        sw.Write(json);
                    }
                }
                return settings;
            }
            else
            {
                // 既存の設定ファイルを読み込み
                json = "";
                using (var fs = new System.IO.FileStream(settingsPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    using (var sr = new StreamReader(fs, System.Text.Encoding .GetEncoding("UTF-8")))
                    {
                        json = sr.ReadToEnd();
                    }
                    JsonConverterCollection converters = new JsonConverterCollection
                    {
                        new SettingsConverter(),
                        new DictionaryGroupConverter(),
                        new DictionaryEntryConverter()
                    };
                    settings = JsonConvert.DeserializeObject<Settings>(json, new JsonSerializerSettings { Converters = converters});
                }
                return settings;
            }
        }
    }
}
