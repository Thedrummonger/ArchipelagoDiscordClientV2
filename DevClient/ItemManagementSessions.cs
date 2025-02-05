using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Data;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace DevClient
{

    internal class ItemManagementSession(DiscordBot discordBot, ulong channelID)
    {
        public static readonly string ManagerMetadataKey = "ItemManagementSessionManager";
        public DiscordBot Bot = discordBot;
        public ulong ChannelID = channelID;
        public Dictionary<string, ArchipelagoSession> ActiveItemClientSessions = [];
        public async void CloseAllConnections()
        {
            ArchipelagoSession[] ActiveSessions = [.. ActiveItemClientSessions.Values];
            foreach (var itemClientSession in ActiveSessions)
            {
                if (itemClientSession.Socket.Connected) { await itemClientSession.Socket.DisconnectAsync(); }
            }
            ActiveItemClientSessions.Clear();
        }
    }
}
