using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Handlers;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    internal class HelpConsoleCommands
    {

        public class HelpConsoleCommand : IConsoleCommand
        {
            public string Name => "help";

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot, string[] Args)
            {
                var t = ConsoleCommandHandlers.ConsoleCommandRegistry.Keys.ToArray();
                Console.WriteLine($"available commands:\n{string.Join("\n", t)}");
            }
        }
    }
}
