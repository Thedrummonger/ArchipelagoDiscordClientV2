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
                var Data = command.GetCommandData();
                if (Data.socketTextChannel is null)
                {
                    await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                    return;
                }

                // Check if the guild and channel have an active session
                if (!discordBot.ActiveSessions.TryGetValue(Data.channelId, out var ActiveSession))
                {
                    await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                    return;
                }
                await PrintSettings(command, ActiveSession);
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
                if (Data.socketTextChannel is null)
                {
                    await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                    return;
                }

                // Check if the guild and channel have an active session
                if (!discordBot.ActiveSessions.TryGetValue(Data.channelId, out var ActiveSession))
                {
                    await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                    return;
                }
                var setting = (int)(Data.GetArg("setting")?.GetValue<long>() ?? 0);
                var value = Data.GetArg("value")?.GetValue<bool?>();

                switch (setting)
                {
                    case (int)SettingEnum.IgnoreLeaveJoin:
                        ActiveSession.settings.IgnoreLeaveJoin = value ?? !ActiveSession.settings.IgnoreLeaveJoin;
                        break;
                    case (int)SettingEnum.IgnoreItemSend:
                        ActiveSession.settings.IgnoreItemSend = value ?? !ActiveSession.settings.IgnoreItemSend;
                        break;
                    case (int)SettingEnum.IgnoreChats:
                        ActiveSession.settings.IgnoreChats = value ?? !ActiveSession.settings.IgnoreChats;
                        break;
                    case (int)SettingEnum.IgnoreConnectedPlayerChats:
                        ActiveSession.settings.IgnoreConnectedPlayerChats = value ?? !ActiveSession.settings.IgnoreConnectedPlayerChats;
                        break;
                    default:
                        await command.RespondAsync($"Invalid option", ephemeral: true);
                        return;
                }
                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.settings;
                File.WriteAllText(Constants.Paths.ConnectionCache, discordBot.ConnectionCache.ToFormattedJson());
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
                var Data = command.GetCommandData();
                if (Data.socketTextChannel is null)
                {
                    await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                    return;
                }

                // Check if the guild and channel have an active session
                if (!discordBot.ActiveSessions.TryGetValue(Data.channelId, out var ActiveSession))
                {
                    await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                    return;
                }
                var add = Data.GetArg("add")?.GetValue<bool>() ?? true;
                var value = Data.GetArg("tags")?.GetValue<string?>() ?? "";
                var values = value.TrimSplit(",").Select(x => x.Trim().ToLower()).ToHashSet();
                foreach (var v in values)
                {
                    if (add) { ActiveSession.settings.IgnoreTags.Add(v); }
                    else { ActiveSession.settings.IgnoreTags.Remove(v); }
                }
                discordBot.ConnectionCache[Data.channelId].Settings = ActiveSession.settings;
                File.WriteAllText(Constants.Paths.ConnectionCache, discordBot.ConnectionCache.ToFormattedJson());
                await PrintSettings(command, ActiveSession);
            }
        }

        public abstract class AppSettingManagementCommand : ICommand
        {
            public abstract string Name { get; }
            public abstract SlashCommandProperties Properties { get; }
            public abstract Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot);
            public async Task PrintSettings(SocketSlashCommand command, Sessions.ActiveBotSession Session)
            {
                await command.RespondAsync($"```json\n{Session.settings.ToFormattedJson()}\n```");
            }
        }
    }
}
