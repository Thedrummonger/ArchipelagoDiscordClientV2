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
                .WithDescription("Goals the current slot").Build();

            public bool IsDebugCommand => true;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out _, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }

                if (!Debugger.IsAttached)
                {
                    await command.RespondAsync("This command is only available for debugging", ephemeral: true);
                    return;
                }
                await command.RespondAsync($"Goaled ${session!.ArchipelagoSession.Players.ActivePlayer.Name} {session.ArchipelagoSession.Players.ActivePlayer.Game}");
                session.ArchipelagoSession.SetGoalAchieved();
            }
        }

        public class DevPrintAppSettingsCommand : ICommand
        {
            public string Name => "dev_print_app_settings";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Prints the application app settings").Build();
            public bool IsDebugCommand => true;

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
