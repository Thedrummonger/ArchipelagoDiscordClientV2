using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class SlashCommandHandlers(DiscordBotData.DiscordBot discordBot)
    {
        public async Task HandleSlashCommand(SocketSlashCommand command)
        {
            var Command = discordBot.commandRegistry.GetCommand(command.CommandName);
            if (Command is null)
            {
                await command.RespondAsync("Unknown Command.", ephemeral: true);
                return;
            }
            await Command.ExecuteCommand(command, discordBot);
        }
    }
}
