using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    public interface IConsoleCommand
    {
        string Name { get; }
        void ExecuteCommand(DiscordBot discordBot, string[] args);
    }
}
