using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using Discord;
using System.Diagnostics;
using ArchipelagoDiscordClientLegacy.Commands;
using TDMUtils;

namespace DevClient.Commands
{
    internal class SettingManagementCommands
    {

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
