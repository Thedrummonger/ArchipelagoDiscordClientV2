namespace ArchipelagoDiscordClientLegacy.Data
{
    public class AppSettings
    {
        public string BotToken = "";
        public int DiscordRateLimitDelay = 100;
        public HashSet<string> IgnoreTags = ["tracker", "textonly"];
        public bool IgnoreLeaveJoin = false;
        public bool IgnoreItemSend = false;
        public bool IgnoreChats = false;
        public bool IgnoreConnectedPlayerChats = true;
    }

    public enum SettingEnum
    {
        IgnoreLeaveJoin = 1,
        IgnoreItemSend = 2,
        IgnoreChats = 3,
        IgnoreConnectedPlayerChats = 4
    }
}
