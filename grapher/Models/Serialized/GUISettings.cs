using grapher.Common;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;

namespace grapher.Models.Serialized
{
    [Serializable]
    [JsonObject]
    public class GUISettings
    {
        #region Constructors

        public GUISettings() {}

        #endregion Constructors

        #region Properties


        [JsonProperty(Order = 1)]
        public int DPI { get; set; }

        [JsonProperty(Order = 2)]
        public int PollRate { get; set; }

        [JsonProperty(Order = 3)]
        public bool ShowLastMouseMove { get; set; }

        [JsonProperty(Order = 4)]
        public bool ShowVelocityAndGain { get; set; }

        [JsonProperty(Order = 5)]
        public bool AutoWriteToDriverOnStartup { get; set; }

        [JsonProperty(
            Order = 6,
            DefaultValueHandling = DefaultValueHandling.Populate
        )]
        [DefaultValue("Light Theme")]
        public string CurrentColorScheme { get; set; }

        [JsonIgnore]
        public static string GuiConfigFileName {
            get {
                return Path.Combine(Helper.GetCfgPath(), ".config");
            }
        }

        #endregion Properties

        #region Methods

        public override bool Equals(object obj)
        {
            var other = obj as GUISettings;

            if (other == null)
            {
                return false;
            }

            return Equals(other);
        }

        public bool Equals(GUISettings other)
        {
            return DPI == other.DPI &&
                PollRate == other.PollRate &&
                ShowLastMouseMove == other.ShowLastMouseMove &&
                ShowVelocityAndGain == other.ShowVelocityAndGain &&
                AutoWriteToDriverOnStartup == other.AutoWriteToDriverOnStartup &&
                CurrentColorScheme == other.CurrentColorScheme;
        }

        public override int GetHashCode()
        {
            return DPI.GetHashCode() ^
                PollRate.GetHashCode() ^
                ShowLastMouseMove.GetHashCode() ^
                ShowVelocityAndGain.GetHashCode() ^
                AutoWriteToDriverOnStartup.GetHashCode() ^
                CurrentColorScheme.GetHashCode();
        }

        public void Save()
        {
            File.WriteAllText(GuiConfigFileName, JsonConvert.SerializeObject(this));
        }

        public static GUISettings MaybeLoad()
        {
            GUISettings settings = null;

            try
            {
                settings = JsonConvert.DeserializeObject<GUISettings>(
                    File.ReadAllText(GuiConfigFileName));
            }
            catch (Exception ex)
            {
                if (!(ex is JsonException || ex is FileNotFoundException))
                {
                    throw;
                }
            }

            return settings;
        }

        #endregion Methods

    }
}
