using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class ShowSessionCommands
    {
        public class ShowChannelSessionCommand : ICommand
        {
            public string Name => "print_channel_session";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Show the active Archipelago session for this channel").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                var Guild = command.GuildId;
                var SessionsInGuild = discordBot.ActiveSessions.Values.Where(x => x.DiscordChannel.Guild.Id == Guild).ToArray();

                // Check if the guild has active sessions
                if (SessionsInGuild.Length == 0)
                {
                    await command.RespondAsync("No active Archipelago sessions in this guild.", ephemeral: true);
                    return;
                }

                var response = "Active Archipelago Sessions:\n";
                foreach (var i in SessionsInGuild)
                {
                    var APSession = i.archipelagoSession;
                    var channel = i.DiscordChannel;
                    if (channel == null) continue;

                    response += $"- **Channel**: {channel.Name}\n" +
                                $"  **Server**: {APSession.Socket.Uri}\n" +
                                $"  **Player**: {APSession.Players.GetPlayerName(APSession.ConnectionInfo.Slot)}({APSession.ConnectionInfo.Slot})\n";
                }

                await command.RespondAsync(response, ephemeral: true);
            }
        }

        public class ShowServerSessionsCommand : ICommand
        {
            public string Name => "print_server_sessions";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Show all active Archipelago sessions in this server").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out _, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }
                var APSession = ActiveSession!.archipelagoSession;
                // Build the response
                var response = $"**Active Archipelago Session**\n" +
                               $"  **Server**: {APSession.Socket.Uri}\n" +
                               $"  **Player**: {APSession.Players.ActivePlayer.Name}({APSession.ConnectionInfo.Slot})\n" +
                               $"  **Game**: {APSession.Players.ActivePlayer.Game}";

                await command.RespondAsync(response, ephemeral: true);
            }
        }
    }
}
