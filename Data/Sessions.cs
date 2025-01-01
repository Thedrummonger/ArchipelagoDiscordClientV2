using Archipelago.MultiClient.Net;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Sessions
    {
        public class ActiveBotSession
        {
            public required SocketTextChannel DiscordChannel;
            public required ArchipelagoSession archipelagoSession;
            public Dictionary<SocketUser, HashSet<string>> SlotAssociations = [];
            public string? OriginalChannelName = null;
        }

        public class ArchipelagoConnectionInfo
        {
            public required string? IP { get; set; }
            public required int Port { get; set; }
            public required string? Game { get; set; }
            public required string? Name { get; set; }
            public required string? Password { get; set; }
        }
    }
}
