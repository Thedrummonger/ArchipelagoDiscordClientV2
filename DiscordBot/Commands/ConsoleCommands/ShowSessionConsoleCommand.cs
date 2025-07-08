using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    internal class ShowSessionConsoleCommand
    {
        public class ShowAllSessionCommands : IConsoleCommand
        {
            public string Name => "print_sessions";

            public void ExecuteCommand(DiscordBotData.DiscordBot discordBot, string[] Args)
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
                    var AuxSlots = ActiveSession.GetAuxiliarySlotNames(true);
                    if (AuxSlots.Length > 0)
                    {
                        response += $"\n**Active Auxiliary Archipelago Session**\n";
                        foreach (var session in AuxSlots.Select(ActiveSession.GetAuxiliarySession))
                        {
                            response +=
                                $"- **Player**: {session!.Players.ActivePlayer.Name}[SLOT:{session.ConnectionInfo.Slot}]\n" +
                                $"  **Game**: {session.Players.ActivePlayer.Game}\n";
                        }
                    }
                }

                Console.WriteLine(response);
            }
        }
    }
}
