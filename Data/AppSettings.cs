namespace ArchipelagoDiscordClientLegacy.Data
{
    public class AppSettings
    {
        public string BotToken = "";
        public int DiscordRateLimitDelay = 500; //Despite what the docs say, the rate limit is 2 messages a second per channel
        public SessionSetting AppDefaultSettings = new() { IgnoreTags = ["tracker", "textonly"] };
    }

    public class SessionSetting
    {
        public HashSet<string> IgnoreTags { get; set; } = [];
        public bool IgnoreLeaveJoin { get; set; } = false;
        public bool IgnoreItemSend { get; set; } = false;
        public bool IgnoreChats { get; set; } = false;
        public bool IgnoreConnectedPlayerChats { get; set; } = true;
        public Dictionary<ulong, HashSet<string>> SlotAssociations { get; set; } = [];
    }

    public enum SettingEnum
    {
        IgnoreLeaveJoin = 1,
        IgnoreItemSend = 2,
        IgnoreChats = 3,
        IgnoreConnectedPlayerChats = 4
    }
}
