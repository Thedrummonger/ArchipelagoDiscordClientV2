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
        public bool IgnoreLeaveJoin { get; set; } = true;
        public bool IgnoreItemSend { get; set; } = false;
        public bool IgnoreHints { get; set; } = false;
        public bool IgnoreChats { get; set; } = false;
        public bool IgnoreConnectedPlayerChats { get; set; } = true;
        public bool IgnoreUnrelated { get; set; } = true;
        public Dictionary<ulong, HashSet<string>> SlotAssociations { get; set; } = [];
    }

    public class ToggleSetting
    {
        public string Key;
        public string Description;
        public Action<SessionSetting, bool?> Execute;
        ToggleSetting(string _Key, string _Desc, Action<SessionSetting, bool?> _Execute) 
        {
            Key = _Key;
            Description = _Desc;
            Execute = _Execute;
        }

        public static readonly ToggleSetting[] ToggleSettings =
        [
            new("IgnoreLeaveJoin", "Ignores client join and client leave messages", (s,v) => s.IgnoreLeaveJoin = v ?? !s.IgnoreLeaveJoin),
            new("IgnoreItemSend", "Ignores messages regarding locations being check and items being received.", (s,v) => s.IgnoreItemSend = v ?? !s.IgnoreItemSend) ,
            new("IgnoreHints", "Ignores messages regarding hints.", (s,v) => s.IgnoreHints = v ?? !s.IgnoreHints) ,
            new("IgnoreChats", "Ignores player chat messages", (s, v) => s.IgnoreChats = v ?? !s.IgnoreChats) ,
            new("IgnoreConnectedPlayerChats", "Ignores chat messages if the connected player " +
                "or any of the Auxiliary Connections are the sender", (s,v) => s.IgnoreConnectedPlayerChats = v ?? !s.IgnoreConnectedPlayerChats) ,
            new("IgnoreUnrelated", "Ignores Item and Hint related messages if the connected player " +
                "or any of the Auxiliary Connections are not the sender or receiver", (s,v) => s.IgnoreUnrelated = v ?? !s.IgnoreUnrelated) ,
        ];
    }
}
