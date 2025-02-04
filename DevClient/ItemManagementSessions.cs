using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Data;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace DevClient
{
    internal class ItemManagementSessionManager
    {
        public static readonly string ManagerMetadataKey = "ItemManagementSessionManager";
        public void ArchipelagoConnectionHelpers_OnSessionCreated(ulong channelId, DiscordBot bot, Sessions.ActiveBotSession session)
        {
            var ItemManagementSession = new ItemManagementSession();
            session.Metadata[ManagerMetadataKey] = ItemManagementSession;
        }

        public async void ArchipelagoConnectionHelpers_OnChannelClosing(ulong channelId, DiscordBot bot)
        {
            if (!bot.ActiveSessions.TryGetValue(channelId, out var session)) return;
            if (!session.Metadata.TryGetValue(ManagerMetadataKey, out var v) || v is not ItemManagementSession itemManagementSession) return;
            ArchipelagoSession[] ActiveSessions = [.. itemManagementSession.ActiveItemClientSessions.Values];
            foreach (var itemClientSession in ActiveSessions)
            {
                if (itemClientSession.Socket.Connected) { await itemClientSession.Socket.DisconnectAsync(); }
            }
            itemManagementSession.ActiveItemClientSessions.Clear();
        }
    }

    internal class ItemManagementSession
    {
        public Dictionary<string, ArchipelagoSession> ActiveItemClientSessions = [];
    }
}
