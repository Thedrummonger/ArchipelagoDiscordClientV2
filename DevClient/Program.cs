using Discord;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace DevClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            BotSocketConfig.SetLogLevel(LogSeverity.Debug);
            await ArchipelagoDiscordClientLegacy.Program.RunBotAsync();
        }
    }
}