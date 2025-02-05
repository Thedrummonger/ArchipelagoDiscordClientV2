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
                ItemManagementSession.CreateItemManagementSession(b, c, s);
            };
            ArchipelagoDiscordClientLegacy.Helpers.ArchipelagoConnectionHelpers.OnChannelClosed += (c, b, s) =>
            {
                ItemManagementSession.CloseItemManagementSessions(s);
            };

            await ArchipelagoDiscordClientLegacy.Program.RunBotAsync();
        }
    }
}