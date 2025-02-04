using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class ShowSessionCommands
    {
        public class ShowSessionCommand : ICommand
        {
            public string Name => "print_session";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .AddOption("show_all", ApplicationCommandOptionType.Boolean, "Print all session in current guild", false)
                .WithDescription("Show the active Archipelago session for this channel").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel commandData, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }
                var ShowAllInGuild = commandData.GetArg("show_all")?.GetValue<bool>() ?? false;
                if (ShowAllInGuild)
                    await ShowGuild(command, discordBot);
                else
                    await ShowChannel(command, ActiveSession!);
            }

            async Task ShowChannel(SocketSlashCommand command, Sessions.ActiveBotSession ActiveSession)
            {
                var APSession = ActiveSession!.ArchipelagoSession;
                // Build the response
                var response = $"**Active Archipelago Session**\n" +
                               $"  **Server**: {APSession.Socket.Uri}\n" +
                               $"  **Player**: {APSession.Players.ActivePlayer.Name}[SLOT:{APSession.ConnectionInfo.Slot}]\n" +
                               $"  **Game**: {APSession.Players.ActivePlayer.Game}";
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

                await command.RespondAsync(response, ephemeral: true);
            }

            async Task ShowGuild(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (command.Channel is not SocketTextChannel)
                {
                    await command.RespondAsync("This channel is not part of a Guild", ephemeral: true);
                    return;
                }

                var Guild = command.GuildId;
                var SessionsInGuild = discordBot.ActiveSessions.Values.Where(x => x.DiscordChannel is SocketTextChannel stc && stc.Guild.Id == Guild).ToArray();

                // Check if the guild has active sessions
                if (SessionsInGuild.Length == 0)
                {
                    await command.RespondAsync("No active Archipelago sessions in this guild.", ephemeral: true);
                    return;
                }

                var response = "Active Archipelago Sessions:\n";
                foreach (var i in SessionsInGuild)
                {
                    var APSession = i.ArchipelagoSession;
                    var channel = i.DiscordChannel;
                    if (channel == null) continue;

                    response += $"- **Channel**: {channel.Name}\n" +
                                $"  **Server**: {APSession.Socket.Uri}\n" +
                                $"  **Player**: {APSession.Players.GetPlayerName(APSession.ConnectionInfo.Slot)}({APSession.ConnectionInfo.Slot})\n\n";
                }

                await command.RespondAsync(response, ephemeral: true);
            }
        }
    }
}
