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
        public static void CreateItemManagementSession(DiscordBot discordBot, ulong channelID, Sessions.ActiveBotSession session)
        {
            var ItemManagementSession = new ItemManagementSession(discordBot, channelID);
            session.Metadata[ManagerMetadataKey] = ItemManagementSession;
        }

        public static async void CloseItemManagementSessions(Sessions.ActiveBotSession session)
        {
            if (!session.Metadata.TryGetValue(ManagerMetadataKey, out var v) || v is not ItemManagementSession itemManagementSession) return;
            ArchipelagoSession[] ActiveSessions = [.. itemManagementSession.ActiveItemClientSessions.Values];
            foreach (var itemClientSession in ActiveSessions)
            {
                if (itemClientSession.Socket.Connected) { await itemClientSession.Socket.DisconnectAsync(); }
            }
            itemManagementSession.ActiveItemClientSessions.Clear();
        }
    }
}
