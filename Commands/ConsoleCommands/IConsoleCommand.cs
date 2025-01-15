using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands
{
    internal interface IConsoleCommand
    {
        string Name { get; }
        void ExecuteCommand(DiscordBot discordBot);
    }
}
