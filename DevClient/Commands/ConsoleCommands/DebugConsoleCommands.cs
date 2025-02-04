using ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Handlers;

namespace DevClient.Commands.ConsoleCommands
{
    internal class DebugConsoleCommands
    {
        public class ShowHeartbeatConsoleCommand : IConsoleCommand
        {
            public string Name => "show_heartbeat";

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot)
            {
                ArchipelagoDiscordClientLegacy.Program.ShowHeartbeat = !ArchipelagoDiscordClientLegacy.Program.ShowHeartbeat;
                Console.WriteLine($"Showing Heartbeat: {ArchipelagoDiscordClientLegacy.Program.ShowHeartbeat}");
            }
        }

        public class HelpConsoleCommand : IConsoleCommand
        {
            public string Name => "help";

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot)
            {
                var t = ConsoleCommandHandlers.ConsoleCommandRegistry.Keys.ToArray();
                Console.WriteLine($"available commands:\n{string.Join("\n", t)}");
            }
        }
    }
}
