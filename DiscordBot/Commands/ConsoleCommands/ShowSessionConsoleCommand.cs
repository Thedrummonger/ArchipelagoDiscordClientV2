using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    internal class ShowSessionConsoleCommand
    {
        public class ShowAllSessionCommands : IConsoleCommand
        {
            public string Name => "print_sessions";

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot)
            {
                var SessionsInGuild = discordBot.ActiveSessions.Values.ToArray();

                // Check if the guild has active sessions
                if (SessionsInGuild.Length == 0)
                {
                    Console.WriteLine("No active Archipelago sessions in this guild.");
                    return;
                }

                var response = "Active Archipelago Sessions:\n";
                foreach (var ActiveSession in SessionsInGuild)
                {
                    string Server;
                    if (ActiveSession.DiscordChannel is SocketTextChannel socketTextChannel) Server = socketTextChannel.Guild.Name;
                    else if (ActiveSession.DiscordChannel is SocketDMChannel socketDMChannel) Server = "Direct Message";
                    else Server = "Unknown";
                    var APSession = ActiveSession.ArchipelagoSession;
                    var channel = ActiveSession.DiscordChannel;
                    if (channel == null) continue;

                    response += $"- **Guild**: {Server}\n" +
                                $"- **Channel**: {channel.Name}\n" +
                                $"  **Server**: {APSession.Socket.Uri}\n" +
                                $"  **Player**: {APSession.Players.GetPlayerName(APSession.ConnectionInfo.Slot)}({APSession.ConnectionInfo.Slot})\n\n";
                    if (ActiveSession.AuxiliarySessions.Count > 0)
                    {
                        response += $"\n**Active Auxiliary Archipelago Session**\n";
                        foreach (var session in ActiveSession.AuxiliarySessions)
                        {
                            response +=
                                $"- **Player**: {session.Value.Players.ActivePlayer.Name}[SLOT:{session.Value.ConnectionInfo.Slot}]\n" +
                                $"  **Game**: {session.Value.Players.ActivePlayer.Game}\n";
                        }
                    }
                }

                Console.WriteLine(response);
            }
        }
    }
}
