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
                    .AddRemoveActionOption()
                    .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
                    .AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true).Build();
            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out var session, out var commandData, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                var actionArg = commandData.GetArg(CommandHelpers.AddRemoveActionName)?.GetValue<long>();
                if (actionArg is not long action)
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }
                else if (action == (int)CommandHelpers.AddRemoveAction.add)
                    await Add(command, discordBot, commandData, session!);
                else
                    await Remove(command, discordBot, commandData, session!);
            }

            async Task Add(SocketSlashCommand command, DiscordBot discordBot, CommandData.CommandDataModel commandData, Sessions.ActiveBotSession session)
            {
                if (!command.Validate(discordBot, out var activeSession, out var data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var user = data.GetArg("user")?.GetValue<SocketUser>();
                var players = data.GetArg("players")?.GetValue<string?>()?.TrimSplit(",").ToHashSet();
                if (user == null || players == null || activeSession == null)
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }

                activeSession.Settings.SlotAssociations!.SetIfEmpty(user!.Id, []);

                var currentAssociations = activeSession.Settings.SlotAssociations[user.Id];
                var apPlayers = activeSession.ArchipelagoSession.Players.AllPlayers.Select(p => p.Name);

                var addedPlayers = new HashSet<string>();
                var invalidPlayers = new HashSet<string>();
                var alreadyAssigned = new HashSet<string>();

                foreach (var player in players)
                {
                    if (!apPlayers.Contains(player)) invalidPlayers.Add(player);
                    else if (currentAssociations.Add(player)) addedPlayers.Add(player);
                    else alreadyAssigned.Add(player);
                }

                List<string> messageParts =
                    [
                    ..addedPlayers.CreateResultList($"The following players were associated with {user!.Username}"),
                    ..alreadyAssigned.CreateResultList($"The following players were already associated with {user!.Username}"),
                    ..invalidPlayers.CreateResultList($"The following players were not valid players in archipelago"),
                    ];

                discordBot.UpdateConnectionCache(data.channelId, activeSession.Settings);
                await command.RespondAsync(string.Join("\n", messageParts));
            }
            async Task Remove(SocketSlashCommand command, DiscordBot discordBot, CommandData.CommandDataModel commandData, Sessions.ActiveBotSession session)
            {
                if (!command.Validate(discordBot, out var activeSession, out var data, out string error))
                {
                    await command.RespondAsync(error, ephemeral: true);
                    return;
                }

                var user = data.GetArg("user")?.GetValue<SocketUser>();
                var players = data.GetArg("players")?.GetValue<string?>()?.TrimSplit(",").ToHashSet();

                if (user == null || players == null || activeSession == null)
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }

                if (!activeSession.Settings.SlotAssociations.TryGetValue(user.Id, out var currentAssociations))
                {
                    await command.RespondAsync($"There are no slot associations for {user.Username}.", ephemeral: true);
                    return;
                }

                var validRemovals = new HashSet<string>();
                var invalidRemovals = new HashSet<string>();

                foreach (var player in players)
                {
                    if (currentAssociations.Remove(player))
                        validRemovals.Add(player);
                    else
                        invalidRemovals.Add(player);
                }

                if (activeSession.Settings.SlotAssociations[user.Id].Count == 0)
                    activeSession.Settings.SlotAssociations.Remove(user.Id);

                List<string> MessageParts =
                    [
                    ..validRemovals.CreateResultList($"The following players were removed from {user!.Username}"),
                    ..invalidRemovals.CreateResultList($"The following players were not associated with {user!.Username}")
                    ];

                discordBot.UpdateConnectionCache(data.channelId, activeSession.Settings);
                await command.RespondAsync(string.Join("\n", MessageParts));
            }
        }
    }
}
