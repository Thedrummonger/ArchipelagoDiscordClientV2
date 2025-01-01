using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
