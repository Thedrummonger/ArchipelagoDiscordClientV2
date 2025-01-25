using System.ComponentModel;
using System.Reflection;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public class AppSettings
    {
        public string BotToken = "";
        public SessionSetting AppDefaultSettings = new() { IgnoreTags = ["tracker", "textonly"] };
    }

    public class SessionSetting
    {
        [Description("Ignores client join and client leave messages")]
        public bool IgnoreLeaveJoin { get; set; } = true;
        [Description("Ignores messages regarding locations being check and items being received.")]
        public bool IgnoreItemSend { get; set; } = false;
        [Description("Ignores messages regarding hints.")]
        public bool IgnoreHints { get; set; } = false;
        [Description("Ignores player chat messages")]
        public bool IgnoreChats { get; set; } = false;
        [Description("Ignores chat messages if the connected player or any of the Auxiliary Connections are the sender")]
        public bool IgnoreConnectedPlayerChats { get; set; } = true;
        [Description("Ignores Item and Hint related messages if the connected player or any of the Auxiliary Connections are not the sender or receiver")]
        public bool IgnoreUnrelated { get; set; } = true;
        public HashSet<string> IgnoreTags { get; set; } = [];
        public Dictionary<ulong, HashSet<string>> SlotAssociations { get; set; } = [];

        public static PropertyInfo[] GetToggleSettings()
        {
            var properties = typeof(SessionSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Where(x => x.PropertyType == typeof(bool)).ToArray();
        }
    }

    public class ToggleSetting(string Name, string Description, SessionSetting settings, PropertyInfo property)
    {
        public string DisplayName = Name;
        public string SettingDescription = Description;
        public bool Value {
            get { return (bool)property.GetValue(settings)!; }
            set { property.SetValue(settings, value); }
        }
    }

    public class SettingsManager
    {
        public readonly List<ToggleSetting> toggleSettings = [];
        public SettingsManager(SessionSetting settings)
        {
            foreach (var property in SessionSetting.GetToggleSettings())
            {
                var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                string description = descriptionAttribute?.Description ?? "No description available";
                toggleSettings.Add(new ToggleSetting(property.Name, description, settings, property));
            }
        }
        public string[] GetSettingNames() => toggleSettings.Select(x => x.DisplayName).ToArray();
        public ToggleSetting? GetSetting(long? index)
        {
            if (index == null) return null;
            return GetSetting((int)index.Value);
        }
        public ToggleSetting? GetSetting(int index)
        {
            if (index < 0 || index >= toggleSettings.Count) return null;
            return toggleSettings[index];
        }
    }
}
