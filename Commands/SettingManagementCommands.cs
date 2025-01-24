﻿using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class SettingManagementCommands
    {
        public class PrintSessionSettingsCommand : ICommand
        {
            public string Name => "print_session_settings";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Prints the current sessions settings").Build();

            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                var SessionCache = discordBot.ConnectionCache[Data.channelId];
                var SettingString = $"```json\n{SessionCache.ToFormattedJson()}\n```";
                await command.RespondAsync(SettingString, ephemeral: true);
            }
        }

        public class ToggleSessionSettings : ICommand
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
                var SelectedToggleSetting = ToggleSetting.ToggleSettings[(int)setting];
                SelectedToggleSetting.ToggleVal(ActiveSession!.Settings, value);

                discordBot.UpdateConnectionCache(Data.channelId);

                await command.RespondAsync($"Setting [{SelectedToggleSetting.Key}] set to [{SelectedToggleSetting.GetVal}]");
            }
        }

        public class EditTagIgnoreList : ICommand
        {
            public string Name => "edit_session_ignored_tags";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Adds or removes ignored Client tags for this session")
                    .AddRemoveActionOption()
                    .AddOption("tags", ApplicationCommandOptionType.String, "Comma-separated tags", true).Build();

            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? ActiveSession, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }
                var actionArg = Data.GetArg(CommandHelpers.AddRemoveActionName)?.GetValue<long>();
                if (actionArg is not long action)
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }
                var value = Data.GetArg("tags")?.GetValue<string?>() ?? "";
                var values = value.TrimSplit(",").Select(x => x.Trim().ToLower()).ToHashSet();
                string ActionResult;
                if (action == (int)CommandHelpers.AddRemoveAction.add)
                {
                    ActiveSession!.Settings.IgnoreTags.UnionWith(values);
                    ActionResult = $"Added [{string.Join(", ", values)}] to the ignore list";
                }
                else
                {
                    ActiveSession!.Settings.IgnoreTags.ExceptWith(values);
                    ActionResult = $"Removed [{string.Join(", ", values)}] from the ignore list";
                }

                discordBot.UpdateConnectionCache(Data.channelId);
                await command.RespondAsync(ActionResult);
            }
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
