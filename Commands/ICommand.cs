using Discord;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public interface ICommand
    {
        string Name { get; }
        SlashCommandProperties Properties { get; }
        Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot);
    }
}
