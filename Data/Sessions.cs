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
    }
}
