using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using TDMUtils;
using ArchipelagoDiscordClientLegacy.Data;
using Discord;

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
                await PrintSettings(command, discordBot);
            }
        }

        public class ToggleAppSettings : AppSettingManagementCommand
        {
            public override string Name => "toggle_bot_settings";

            public override SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Toggle the selected bot setting")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("setting")
                        .WithDescription("Select a setting to toggle")
                        .WithRequired(true)
                        .AddChoice("ignore_leave_join", (int)SettingEnum.IgnoreLeaveJoin)
                        .AddChoice("ignore_item_send", (int)SettingEnum.IgnoreItemSend)
                        .AddChoice("ignore_chats", (int)SettingEnum.IgnoreChats)
                        .AddChoice("ignore_connected_player_chats", (int)SettingEnum.IgnoreConnectedPlayerChats)
                        .WithType(ApplicationCommandOptionType.Integer)
                    )
                    .AddOption("value", ApplicationCommandOptionType.Boolean, "Value", false).Build();

            public override async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                var Data = command.GetCommandData();
                var setting = (int)(Data.GetArg("setting")?.GetValue<long>() ?? 0);
                var value = Data.GetArg("value")?.GetValue<bool?>();

                switch (setting)
                {
                    case (int)SettingEnum.IgnoreLeaveJoin:
                        discordBot.appSettings.IgnoreLeaveJoin = value ?? !discordBot.appSettings.IgnoreLeaveJoin;
                        break;
                    case (int)SettingEnum.IgnoreItemSend:
                        discordBot.appSettings.IgnoreItemSend = value ?? !discordBot.appSettings.IgnoreItemSend;
                        break;
                    case (int)SettingEnum.IgnoreChats:
                        discordBot.appSettings.IgnoreChats = value ?? !discordBot.appSettings.IgnoreChats;
                        break;
                    case (int)SettingEnum.IgnoreConnectedPlayerChats:
                        discordBot.appSettings.IgnoreConnectedPlayerChats = value ?? !discordBot.appSettings.IgnoreConnectedPlayerChats;
                        break;
                    default:
                        await command.RespondAsync($"Invalid option", ephemeral: true);
                        return;
                }
                File.WriteAllText(Constants.Paths.ConfigFile, discordBot.appSettings.ToFormattedJson());
                await PrintSettings(command, discordBot);
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
                var Data = command.GetCommandData();
                var add = Data.GetArg("add")?.GetValue<bool>() ?? true;
                var value = Data.GetArg("tags")?.GetValue<string?>() ?? "";
                var values = value.TrimSplit(",").Select(x => x.Trim().ToLower()).ToHashSet();
                foreach (var v in values)
                {
                    if (add) { discordBot.appSettings.IgnoreTags.Add(v); }
                    else { discordBot.appSettings.IgnoreTags.Remove(v); }
                }
                File.WriteAllText(Constants.Paths.ConfigFile, discordBot.appSettings.ToFormattedJson());
                await PrintSettings(command, discordBot);
            }
        }

        public abstract class AppSettingManagementCommand : ICommand
        {
            public abstract string Name { get; }
            public abstract SlashCommandProperties Properties { get; }
            public abstract Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot);
            public async Task PrintSettings(SocketSlashCommand command, DiscordBot discordBot)
            {
                var SettingsCopy = discordBot.appSettings.DeepClone();
                SettingsCopy.BotToken = null; //Remove the bot token from the print
                await command.RespondAsync($"```json\n{SettingsCopy.ToFormattedJson()}\n```");
            }
        }
    }
}
