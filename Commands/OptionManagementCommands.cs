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
        public class PrintAppSettingsCommand : AppSettingManagementCommand
        {
            public override string Name => "show_bot_settings";

            public override SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Prints the current bot settings").Build();

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                await PrintSettings(command, session!);
            }
        }

        public class ToggleAppSettings : AppSettingManagementCommand
        {
            public override string Name => "toggle_bot_settings";

            public override SlashCommandProperties Properties
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

                    return new SlashCommandBuilder()
                        .WithName(Name)
                        .WithDescription("Toggle the selected bot setting")
                        .AddOption(optionBuilder)
                        .AddOption("value", ApplicationCommandOptionType.Boolean, "Value", false).Build();
                }
            }

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }
                var setting = (Data.GetArg("setting")?.GetValue<long>());
                var value = Data.GetArg("value")?.GetValue<bool?>();

                if (setting is null || setting < 0 || setting >= ToggleSetting.ToggleSettings.Length)
                {
                    await command.RespondAsync("Invalid Option", ephemeral: true);
                    return;
                }
                ToggleSetting.ToggleSettings[(int)setting].Execute(ActiveSession!.Settings, value);

                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.Settings;
                discordBot.UpdateConnectionCache();
                await PrintSettings(command, ActiveSession);
            }
        }

        public class EditTagIgnoreList : AppSettingManagementCommand
        {
            public override string Name => "edit_ignored_tags";

            public override SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Adds or removes ignored tags")
                    .AddOption("add", ApplicationCommandOptionType.Boolean, "true: add, false: remove", true)
                    .AddOption("tags", ApplicationCommandOptionType.String, "Comma-separated tags", true).Build();

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
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

                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession!.Settings;
                discordBot.UpdateConnectionCache();
                await PrintSettings(command, ActiveSession);
            }
        }

        public abstract class AppSettingManagementCommand : ICommand
        {
            public abstract string Name { get; }
            public abstract SlashCommandProperties Properties { get; }
            public bool IsDebugCommand => false;
            public abstract Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot);
            public async Task PrintSettings(SocketSlashCommand command, Sessions.ActiveBotSession Session)
            {
                await command.RespondAsync($"```json\n{Session.Settings.ToFormattedJson()}\n```");
            }
        }

        public class SettingDetailsCommand : ICommand
        {
            public string Name => "print_setting_details";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Prints a description of each setting").Build();
            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                List<string> Print = ToggleSetting.ToggleSettings.Select(x => $"{x.Key}: {x.Description}").ToList();
                Print.Add("edit_ignored_tags: Ignore tags will ignore a client message if the message tags contain an ignore tag");
                await command.RespondAsync(string.Join("\n", Print));
            }
        }
    }
}
