using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    internal interface IConsoleCommand
    {
        string Name { get; }
        void ExecuteCommand(DiscordBot discordBot);
    }
}
