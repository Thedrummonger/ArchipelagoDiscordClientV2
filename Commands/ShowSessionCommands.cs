using ArchipelagoDiscordClient;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class ShowSessionCommands
    {
        public static async Task HandleShowSessionsCommand(SocketSlashCommand command, DiscordBot discordBot)
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

        public static async Task HandleShowChannelSessionCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            var Data = command.GetCommandData();
            // Check if the channel has an active session
            if (!discordBot.ActiveSessions.ContainsKey(Data.channelId))
            {
                await command.RespondAsync("No active Archipelago session in this channel.", ephemeral: true);
                return;
            }
            var APSession = discordBot.ActiveSessions[Data.channelId].archipelagoSession;

            // Build the response
            var response = $"**Active Archipelago Session**\n" +
                           $"  **Server**: {APSession.Socket.Uri}\n" +
                           $"  **Player**: {APSession.Players.GetPlayerName(APSession.ConnectionInfo.Slot)}({APSession.ConnectionInfo.Slot})\n";

            await command.RespondAsync(response, ephemeral: true);
        }
    }
}
