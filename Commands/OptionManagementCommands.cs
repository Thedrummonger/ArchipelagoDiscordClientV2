using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class AppSettingManagementCommands
    {
        public class PrintAppSettingsCommand : ICommand
        {
            public string Name => "print_session_settings";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Prints the current bot settings").Build();

            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                await PrintSettings(command, session!);
            }
        }

        public class ToggleAppSettings : ICommand
        {
            public string Name => "toggle_session_settings";

            public SlashCommandProperties Properties
            {
                get
                {
                    var optionBuilder = new SlashCommandOptionBuilder()
                            .WithName("setting")
                            .WithDescription("Select a setting to toggle")
                            .WithRequired(true)
                            .WithType(ApplicationCommandOptionType.Integer);
                    for (int index = 0; index < ToggleSetting.ToggleSettings.Length; index++)
                    {
                        var value = ToggleSetting.ToggleSettings[index];
                        optionBuilder.AddChoice(value.Key, index);
                    }
                    optionBuilder.AddChoice("help", ToggleSetting.ToggleSettings.Length);
                    return new SlashCommandBuilder()
                        .WithName(Name)
                        .WithDescription("Toggle the selected setting for this session")
                        .AddOption(optionBuilder)
                        .AddOption("value", ApplicationCommandOptionType.Boolean, "Value", false).Build();
                }
            }

            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }
                var setting = (Data.GetArg("setting")?.GetValue<long>());
                var value = Data.GetArg("value")?.GetValue<bool?>();

                if (setting is null || setting < 0 || setting > ToggleSetting.ToggleSettings.Length)
                {
                    await command.RespondAsync("Invalid Option", ephemeral: true);
                    return;
                }

                if (setting == ToggleSetting.ToggleSettings.Length)
                {
                    await PrintHelp(command);
                    return;
                }

                ToggleSetting.ToggleSettings[(int)setting].Execute(ActiveSession!.Settings, value);

                discordBot.UpdateConnectionCache(Data.channelId, ActiveSession.Settings);
                await PrintSettings(command, ActiveSession);
            }
        }

        public class EditTagIgnoreList : ICommand
        {
            public string Name => "edit_session_ignored_tags";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Adds or removes ignored Client tags for this session")
                    .AddOption("add", ApplicationCommandOptionType.Boolean, "true: add, false: remove", true)
                    .AddOption("tags", ApplicationCommandOptionType.String, "Comma-separated tags", true).Build();

            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }
                var add = Data.GetArg("add")?.GetValue<bool>() ?? true;
                var value = Data.GetArg("tags")?.GetValue<string?>() ?? "";
                var values = value.TrimSplit(",").Select(x => x.Trim().ToLower()).ToHashSet();
                if (add)
                    ActiveSession!.Settings.IgnoreTags.UnionWith(values);
                else
                    ActiveSession!.Settings.IgnoreTags.ExceptWith(values);

                discordBot.UpdateConnectionCache(Data.channelId, ActiveSession!.Settings);
                await PrintSettings(command, ActiveSession);
            }
        }

        private static async Task PrintSettings(SocketSlashCommand command, Sessions.ActiveBotSession Session)
        {
            await command.RespondAsync($"```json\n{Session.Settings.ToFormattedJson()}\n```");
        }

        private static async Task PrintHelp(SocketSlashCommand command)
        {
            List<string> Print = ToggleSetting.ToggleSettings.Select(x => $"{x.Key}: {x.Description}").ToList();
            Print.Add("edit_ignored_tags: connect/disconnect messages will be ignored if the clients tags contain an ignored tag." +
                "Default Ignored tags are\n\"tracker\",\n\"textonly\"");
            await command.RespondAsync(string.Join("\n", Print));
        }
    }
}
