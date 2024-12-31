using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

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
