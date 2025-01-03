namespace ArchipelagoDiscordClientLegacy.Data
{
    public class AppSettings
    {
        public string BotToken = "";
        public int DiscordRateLimitDelay = 100;
        public SessionSetting AppDefaultSettings = new SessionSetting() { IgnoreTags = ["tracker", "textonly"] };
    }

    public class SessionSetting
    {
        public HashSet<string> IgnoreTags { get; set; } = [];
        public bool IgnoreLeaveJoin { get; set; } = false;
        public bool IgnoreItemSend { get; set; } = false;
        public bool IgnoreChats { get; set; } = false;
        public bool IgnoreConnectedPlayerChats { get; set; } = true;
    }

    public enum SettingEnum
    {
        IgnoreLeaveJoin = 1,
        IgnoreItemSend = 2,
        IgnoreChats = 3,
        IgnoreConnectedPlayerChats = 4
    }
}
