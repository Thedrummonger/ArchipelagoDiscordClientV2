using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using TDMUtils;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    internal class DevCommands
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

        public class DevPrintAppSettingsCommand : ICommand
        {
            public string Name => "dev_print_app_settings";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Prints the application app settings").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!Debugger.IsAttached)
                {
                    await command.RespondAsync("This command is only available for debugging", ephemeral: true);
                    return;
                }
                await command.RespondAsync($"```json\n{discordBot.appSettings.ToFormattedJson()}\n```");
            }
        }
    }
}
