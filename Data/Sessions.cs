using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Handlers;
using Discord.WebSocket;
using TDMUtils;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Sessions
    {
        public class ActiveBotSession
        {
            public ActiveBotSession(SessionConstructor sessionConstructor, DiscordBotData.DiscordBot parent, ISocketMessageChannel channel, ArchipelagoSession APSession)
            {
                Settings = sessionConstructor.Settings!.DeepClone();
                MessageQueue = new ActiveSessionMessageQueue(parent, this);
                ConnectionInfo = sessionConstructor.ArchipelagoConnectionInfo!.DeepClone();
                DiscordChannel = channel;
                ArchipelagoSession = APSession;
                AuxiliarySessions = [];
                ParentBot = parent;
            }
            public DiscordBotData.DiscordBot ParentBot { get; private set; }
            public ISocketMessageChannel DiscordChannel { get; private set; }
            public ArchipelagoSession ArchipelagoSession { get; private set; }
            public Dictionary<string, ArchipelagoSession> AuxiliarySessions { get; private set; }
            public SessionSetting Settings { get; private set; }
            public ArchipelagoConnectionInfo ConnectionInfo { get; private set; }
            public ActiveSessionMessageQueue MessageQueue { get; private set; }
        }

        public class ArchipelagoConnectionInfo
        {
            public required string? IP { get; set; }
            public required int Port { get; set; }
            public required string? Game { get; set; }
            public required string? Name { get; set; }
            public required string? Password { get; set; }
        }

        public class SessionConstructor
        {
            public ArchipelagoConnectionInfo? ArchipelagoConnectionInfo { get; set; }
            public SessionSetting? Settings { get; set; }
        }
    }
}
