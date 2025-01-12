using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public class UserAssignmentCommands
    {
        public class UnAssignUserCommand : ICommand
        {
            public string Name => "detach_user_from_player";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Detaches discord user from archipelago player")
                    .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
                    .AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var user = Data.GetArg("user")?.GetValue<SocketUser>();
                var players = Data.GetArg("players")?.GetValue<string?>();

                if (!ActiveSession!.settings.SlotAssociations.ContainsKey(user!.Id!))
                {
                    await command.RespondAsync($"There are no slot associations for {user!.Username}.", ephemeral: true);
                    return;
                }

                var PlayerList = players!.TrimSplit(",").ToHashSet();

                HashSet<string> valid = [];
                HashSet<string> invalid = [];
                foreach (var player in PlayerList) 
                {
                    bool WasRemoved = ActiveSession.settings.SlotAssociations[user!.Id].Remove(player);
                    HashSet<string> trackingList = WasRemoved ? valid : invalid;
                    trackingList.Add(player);
                }

                List<string> MessageParts = 
                    [
                    ..valid.CreateResultList($"The following players were removed from {user!.Username}"),
                    ..invalid.CreateResultList($"The following players were not associated with {user!.Username}")
                    ];
                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.settings;
                discordBot.UpdateConnectionCache();
                await command.RespondAsync(String.Join("\n", MessageParts));
            }
        }

        public class AssignUserCommand : ICommand
        {
            public string Name => "assign_user_to_player";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Assign discord user to archipelago player")
                    .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
                    .AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var user = Data.GetArg("user")?.GetValue<SocketUser>();
                var players = Data.GetArg("players")?.GetValue<string?>();

                var APPlayers = ActiveSession!.archipelagoSession.Players.AllPlayers.Select(p => p.Name);

                ActiveSession.settings.SlotAssociations!.SetIfEmpty(user!.Id, []);

                var CurrentAssociations = ActiveSession.settings.SlotAssociations[user!.Id];

                var PlayerList = players!.TrimSplit(",").ToHashSet(); //Players passed by the command

                HashSet<string> AddedPlayers = [];
                HashSet<string> InvalidPlayers = [];
                HashSet<string> AlreadyAssigned = [];
                foreach (var Player in PlayerList) 
                { 
                    if (!APPlayers.Contains(Player)) 
                    { 
                        InvalidPlayers.Add(Player); 
                        continue; 
                    }
                    var WasAdded = CurrentAssociations.Add(Player);
                    var UpdateList = WasAdded ? AddedPlayers : AlreadyAssigned;
                }

                List<string> MessageParts = 
                    [
                    ..PlayerList.CreateResultList($"The following players were associated to {user!.Username}"),
                    ..AlreadyAssigned.CreateResultList($"The following players were already assigned to {user!.Username}"),
                    ..InvalidPlayers.CreateResultList($"The following players were not valid players in archipelago"),
                    ];

                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.settings;
                discordBot.UpdateConnectionCache();

                await command.RespondAsync(String.Join("\n", MessageParts));
            }
        }
    }
}
