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
            return properties.Where(x => x.PropertyType == typeof(bool)).ToArray();
        }
    }

    /// <summary>
    /// Represents a toggleable session setting with a display name and description.
    /// </summary>
    public class ToggleSetting(string Name, string Description, SessionSetting settings, PropertyInfo property)
    {
        public string DisplayName = Name;
        public string SettingDescription = Description;
        public bool Value
        {
            get { return (bool)property.GetValue(settings)!; }
            set { property.SetValue(settings, value); }
        }
    }

    /// <summary>
    /// Manages session settings and provides methods for retrieving and modifying toggleable settings.
    /// </summary>
    public class SettingsManager
    {
        /// <summary>
        /// A list of available toggle settings.
        /// </summary>
        public readonly List<ToggleSetting> toggleSettings = [];
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsManager"/> class and populates the toggle settings list.
        /// </summary>
        /// <param name="settings">The session settings to manage.</param>
        public SettingsManager(SessionSetting settings)
        {
            foreach (var property in SessionSetting.GetToggleSettings())
            {
                var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                string description = descriptionAttribute?.Description ?? "No description available";
                toggleSettings.Add(new ToggleSetting(property.Name, description, settings, property));
            }
        }
        /// <summary>
        /// Retrieves a toggle setting by its index.
        /// </summary>
        /// <typeparam name="T">The numeric type (e.g., int, long, short, double, int?, long?).</typeparam>
        /// <param name="index">The numeric index of the setting.</param>
        /// <returns>The corresponding <see cref="ToggleSetting"/> or null if the index is invalid.</returns>
        public ToggleSetting? GetSetting<T>(T? index) where T : struct, IConvertible
        {
            if (index is null) return null;
            try
            {
                int parsedIndex = Convert.ToInt32(index);
                return (parsedIndex >= 0 && parsedIndex < toggleSettings.Count) ? toggleSettings[parsedIndex] : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
