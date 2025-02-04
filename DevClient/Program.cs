using Discord;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace DevClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BotSocketConfig.SetLogLevel(LogSeverity.Debug);

            ItemManagementSessionManager itemManagementSessionManager = new();
            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnSessionCreated += 
                itemManagementSessionManager.ArchipelagoConnectionHelpers_OnSessionCreated;
            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnChannelClosing += 
                itemManagementSessionManager.ArchipelagoConnectionHelpers_OnChannelClosing;

            await ArchipelagoDiscordClientLegacy.Program.RunBotAsync();
        }
    }
}