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

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot, string[] Args)
            {
                ArchipelagoDiscordClientLegacy.Program.ShowHeartbeat = !ArchipelagoDiscordClientLegacy.Program.ShowHeartbeat;
                Console.WriteLine($"Showing Heartbeat: {ArchipelagoDiscordClientLegacy.Program.ShowHeartbeat}");
            }
        }
    }
}
