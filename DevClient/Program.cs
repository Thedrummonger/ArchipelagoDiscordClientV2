using Discord;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace DevClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BotSocketConfig.SetLogLevel(LogSeverity.Debug);

            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnSessionCreated += (s) =>
            {
                s.Metadata[ItemManagementSession.ManagerMetadataKey] = new ItemManagementSession(s.ParentBot, s.DiscordChannel.Id);
            };
            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnChannelClosed += (s) =>
            {
                if (s.Metadata.TryGetValue(ItemManagementSession.ManagerMetadataKey, out var v) && v is ItemManagementSession IMSession)
                    IMSession.CloseAllConnections();
            };

            await ArchipelagoDiscordClientLegacy.Program.RunBotAsync(args);
        }
    }
}