using Discord;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace DevClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BotSocketConfig.SetLogLevel(LogSeverity.Debug);

            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnSessionCreated += (c, b, s) =>
            {
                s.Metadata[ItemManagementSession.ManagerMetadataKey] = new ItemManagementSession(b, c);
            };
            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnChannelClosed += (c, b, s) =>
            {
                if (s.Metadata.TryGetValue(ItemManagementSession.ManagerMetadataKey, out var v) && v is ItemManagementSession IMSession)
                    IMSession.CloseAllConnections();
            };

            await ArchipelagoDiscordClientLegacy.Program.RunBotAsync(args);
        }
    }
}