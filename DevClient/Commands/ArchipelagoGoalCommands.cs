using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using Discord;
using System.Diagnostics;
using ArchipelagoDiscordClientLegacy.Commands;
using ArchipelagoDiscordClientLegacy.Helpers;

namespace DevClient.Commands
{
    internal class ArchipelagoGoalCommands
    {
        public class DevGoalCommand : ICommand
        {
            public string Name => "dev_goal";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .AddOption("player", ApplicationCommandOptionType.String, "Player to goal", false)
                .WithDescription("Goals the current slot").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out var session, out var commandData, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }

                if (!Debugger.IsAttached)
                {
                    await command.RespondAsync("This command is only available for debugging", ephemeral: true);
                    return;
                }
                string ActivePlayerName = session!.ArchipelagoSession.Players.ActivePlayer.Name;

                var player = commandData.GetArg("player")?.GetValue<string>() ?? ActivePlayerName;
                ArchipelagoSession SelectedAPSession;
                if (player == ActivePlayerName) SelectedAPSession = session.ArchipelagoSession;
                else if (session.AuxiliarySessions.TryGetValue(player, out var AuxSession)) SelectedAPSession = AuxSession;
                else
                {
                    await command.RespondAsync($"{player} is not a valid connected slot", ephemeral: true);
                    return;
                }

                await command.RespondAsync($"Goaled ${SelectedAPSession.Players.ActivePlayer.Name} {SelectedAPSession.Players.ActivePlayer.Game}");
                SelectedAPSession.SetGoalAchieved();
            }
        }

        public class DevSendLocationCommand : ICommand
        {
            public string Name => "dev_send_location";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .AddOption("player", ApplicationCommandOptionType.String, "player whose world contains the location", true)
                .AddOption("location_name", ApplicationCommandOptionType.String, "location to check", true)
                .WithDescription("Marks the given location as checked").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out var session, out var commandData, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }

                if (!Debugger.IsAttached)
                {
                    await command.RespondAsync("This command is only available for debugging", ephemeral: true);
                    return;
                }

                if (!session!.Metadata.TryGetValue(ItemManagementSessionManager.ManagerMetadataKey, out var v) || v is not ItemManagementSession itemManagementSession)
                {
                    await command.RespondAsync("Unhandled error, ItemManagementSession Metadata did not exist", ephemeral: true);
                    return;
                }

                var playerArg = commandData.GetArg("player")?.GetValue<string>();
                if (playerArg is null || !itemManagementSession.ActiveItemClientSessions.TryGetValue(playerArg, out var SelectedSession))
                {
                    await command.RespondAsync($"No valid Item Management connection for {playerArg}", ephemeral: true);
                    return;
                }

                var locationArg = commandData.GetArg("location_name")?.GetValue<string>();
                var LocationID = locationArg is null ? -1 : SelectedSession.Locations.GetLocationIdFromName(SelectedSession.Players.ActivePlayer.Game, locationArg);

                if (LocationID < 0)
                {
                    await command.RespondAsync($"{locationArg} was not a valid location for game {SelectedSession.Players.ActivePlayer.Game}", ephemeral: true);
                    return;
                }

                SelectedSession.Locations.CompleteLocationChecks(LocationID);
                await command.RespondAsync($"Check location [{locationArg}] for slot [{playerArg}] playing [{SelectedSession.Players.ActivePlayer.Game}]", ephemeral: true);
            }
        }
    }
}
