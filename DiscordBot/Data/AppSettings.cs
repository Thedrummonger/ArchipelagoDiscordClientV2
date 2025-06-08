using System.ComponentModel;
using System.Reflection;

namespace ArchipelagoDiscordClientLegacy.Data
{
    /// <summary>
    /// Represents the bot's application-wide settings, including default session configurations.
    /// </summary>
    public class AppSettings
    {
        public string BotToken = "";
        public SessionSetting AppDefaultSettings = new() { IgnoreTags = ["tracker", "textonly"] };
    }

    /// <summary>
    /// Represents configurable session settings that control message filtering and behavior.
    /// </summary>
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

        /// <summary>
        /// Retrieves all boolean-based settings that can be toggled.
        /// </summary>
        /// <returns>An array of boolean property metadata.</returns>
        public static PropertyInfo[] GetToggleSettings()
        {
            var properties = typeof(SessionSetting).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return [.. properties.Where(x => x.PropertyType == typeof(bool))];
        }
        /// <summary>
        /// Gets a container with the specific instance of a given toggle from this instance of SessionSetting
        /// </summary>
        public ToggleSetting? GetSetting<T>(T? index) where T : struct, IConvertible
        {
            if (index is null) return null;
            var toggleSettings = SessionSetting.GetToggleSettings();
            try
            {
                int parsedIndex = Convert.ToInt32(index);
                return (parsedIndex >= 0 && parsedIndex < toggleSettings.Length) ? new ToggleSetting(this, toggleSettings[parsedIndex]) : null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// A container containing a specific instance of a toggle setting from the given instance of SessionSetting.
    /// </summary>
    public class ToggleSetting(SessionSetting settings, PropertyInfo property)
    {
        public string DisplayName = property.Name;
        public string SettingDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "No description available";
        public bool Value
        {
            get { return (bool)property.GetValue(settings)!; }
            set { property.SetValue(settings, value); }
        }
    }
}
