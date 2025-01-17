using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public class UserAssignmentCommands
    {
        public class UnAssignUserCommand : ICommand
        {
            public string Name => "edit_user_assignments";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Detaches discord user from archipelago player")
                    .AddOption("add", ApplicationCommandOptionType.User, "True: Add, False: Remove", true)
                    .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
                    .AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true).Build();
            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                var actionArg = commandData.GetArg("add")?.GetValue<bool>();
                if (actionArg is not bool action)
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }
                else if (action)
                    await Add(command, discordBot, commandData, session!);
                else
                    await Remove(command, discordBot, commandData, session!);
            }

            async Task Add(SocketSlashCommand command, DiscordBot discordBot, CommandData.CommandDataModel commandData, Sessions.ActiveBotSession session)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var user = Data.GetArg("user")?.GetValue<SocketUser>();
                var players = Data.GetArg("players")?.GetValue<string?>();

                var APPlayers = ActiveSession!.ArchipelagoSession.Players.AllPlayers.Select(p => p.Name);

                ActiveSession.Settings.SlotAssociations!.SetIfEmpty(user!.Id, []);

                var CurrentAssociations = ActiveSession.Settings.SlotAssociations[user!.Id];

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

                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.Settings;
                discordBot.UpdateConnectionCache();

                await command.RespondAsync(String.Join("\n", MessageParts));
            }
            async Task Remove(SocketSlashCommand command, DiscordBot discordBot, CommandData.CommandDataModel commandData, Sessions.ActiveBotSession session)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var user = Data.GetArg("user")?.GetValue<SocketUser>();
                var players = Data.GetArg("players")?.GetValue<string?>();

                if (!ActiveSession!.Settings.SlotAssociations.ContainsKey(user!.Id!))
                {
                    await command.RespondAsync($"There are no slot associations for {user!.Username}.", ephemeral: true);
                    return;
                }

                var PlayerList = players!.TrimSplit(",").ToHashSet();

                HashSet<string> valid = [];
                HashSet<string> invalid = [];
                foreach (var player in PlayerList)
                {
                    bool WasRemoved = ActiveSession.Settings.SlotAssociations[user!.Id].Remove(player);
                    HashSet<string> trackingList = WasRemoved ? valid : invalid;
                    trackingList.Add(player);
                }

                List<string> MessageParts =
                    [
                    ..valid.CreateResultList($"The following players were removed from {user!.Username}"),
                    ..invalid.CreateResultList($"The following players were not associated with {user!.Username}")
                    ];
                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.Settings;
                discordBot.UpdateConnectionCache();
                await command.RespondAsync(String.Join("\n", MessageParts));
            }
        }
    }
}
