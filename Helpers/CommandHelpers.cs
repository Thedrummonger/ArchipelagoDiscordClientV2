using Archipelago.MultiClient.Net.MessageLog.Parts;
using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class CommandHelpers
    {
        public static bool Validate(
            this SocketSlashCommand command, 
            DiscordBot discordBot, 
            bool CheckConnected, 
            out CommandData.CommandDataModel commandData,
            out string Error
            )
        {
            Error = "Unknown Error";
            commandData = command.GetCommandData();
            if (commandData.socketTextChannel is null)
            {
                Error = "Only Text Channels are Supported";
            }

            if (discordBot.ActiveSessions.ContainsKey(commandData.channelId) != CheckConnected)
            {
                Error = CheckConnected ? 
                    "This channel is not connected to an Archipelago session." : 
                    "This channel is already connected to an Archipelago session.";
                return false;
            }
            return true;
        }
        public static bool Validate(
            this SocketSlashCommand command,
            DiscordBot discordBot,
            out Sessions.ActiveBotSession? session,
            out CommandData.CommandDataModel commandData,
            out string Result
            )
        {
            session = null;
            if (!Validate(command, discordBot, true, out commandData, out Result)) return false;
            if (!discordBot.ActiveSessions.TryGetValue(commandData.channelId, out session)) return false; //Should never be false
            return true;
        }

        public static string[] CreateResultList(this IEnumerable<string> Values, string Status)
        {
            if (!Values.Any()) return [];
            return [Status, .. Values.Select(x => $"-{x}")];
        }
    }
}
