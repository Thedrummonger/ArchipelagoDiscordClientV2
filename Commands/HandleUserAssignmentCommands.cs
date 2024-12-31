using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    internal class HandleUserAssignmentCommands
    {
        public static async Task HandleAssignUserToPlayer(SocketSlashCommand command, DiscordBot discordBot)
        {
            var Data = command.GetCommandData();
            if (Data.socketTextChannel is null)
            {
                await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                return;
            }

            // Check if the guild and channel have an active session
            if (!discordBot.ActiveSessions.TryGetValue(Data.channelId, out var session))
            {
                await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                return;
            }

            var APSession = discordBot.ActiveSessions[Data.channelId];

            var user = Data.GetArg("user")?.GetValue<SocketUser>();
            var players = Data.GetArg("players")?.GetValue<string?>();

            var APPlayers = APSession.archipelagoSession.Players.AllPlayers.Select(p => p.Name);

            APSession.SlotAssociations!.SetIfEmpty(user, []);

            var CurrentAssociations =  APSession.SlotAssociations[user!];

            var PlayerList = players!.TrimSplit(",").ToHashSet(); //Players passed by the command
            var AlreadyAssigned = PlayerList.Where(CurrentAssociations.Contains).ToHashSet(); //Players already assigned to this user
            var InvalidPlayers = PlayerList.Where(x => !APPlayers.Contains(x)).ToHashSet(); //Players not found in AP
            PlayerList = PlayerList.Where(x => !AlreadyAssigned.Contains(x) && !InvalidPlayers.Contains(x)).ToHashSet(); //Valid Players

            foreach(var Player in PlayerList) { CurrentAssociations.Add(Player); }

            List<string> MessageParts = [];
            if (PlayerList.Count > 0)
            {
                MessageParts.Add($"The following players were associated to {user!.Username}");
                foreach (var player in PlayerList)
                {
                    MessageParts.Add($"-{player}");
                }
            }
            if (AlreadyAssigned.Count > 0)
            {
                MessageParts.Add($"The following players were already assigned to {user!.Username}");
                foreach (var player in AlreadyAssigned)
                {
                    MessageParts.Add($"-{player}");
                }
            }
            if (InvalidPlayers.Count > 0)
            {
                MessageParts.Add($"The following players were not valid players in archipelago");
                foreach (var player in InvalidPlayers)
                {
                    MessageParts.Add($"-{player}");
                }
            }

            await command.RespondAsync(String.Join("\n", MessageParts));

        }
    }
}
