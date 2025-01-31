using ArchipelagoDiscordClient;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Handlers;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    internal class DebugConsoleCommands
    {
        public class ShowHeartbeatConsoleCommand : IConsoleCommand
        {
            public string Name => "show_heartbeat";

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot)
            {
                Program.ShowHeartbeat = !Program.ShowHeartbeat;
                Console.WriteLine($"Showing Heartbeat: {Program.ShowHeartbeat}");
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
