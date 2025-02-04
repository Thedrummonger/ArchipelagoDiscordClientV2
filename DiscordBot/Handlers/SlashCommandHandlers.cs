using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    /// <summary>
    /// Handles incoming Discord slash commands and executes registered commands.
    /// </summary>
    /// <param name="discordBot">The Discord bot instance managing the commands.</param>
    public class SlashCommandHandlers(DiscordBotData.DiscordBot discordBot)
    {
        /// <summary>
        /// Processes an incoming slash command and executes the corresponding registered command.
        /// </summary>
        /// <param name="command">The incoming Discord slash command.</param>
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
