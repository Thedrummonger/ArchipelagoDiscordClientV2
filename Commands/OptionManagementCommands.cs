using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using TDMUtils;
using ArchipelagoDiscordClientLegacy.Data;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class OptionManagementCommands
    {
        public static async Task PrintSettings(SocketSlashCommand command, DiscordBot discordBot)
        {
            var SettingsCopy = discordBot.appSettings.DeepClone();
            SettingsCopy.BotToken = null; //Remove the bot token from the print
            await command.RespondAsync($"```json\n{SettingsCopy.ToFormattedJson()}\n```");
        }
        public static async Task HandleShowSettingsCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            await PrintSettings(command, discordBot);
        }
        public static async Task HandleChangeSettingsCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            var Data = command.GetCommandData();
            var setting = (int)(Data.GetArg("setting")?.GetValue<long>()??0);
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
        public static async Task HandleEditIngoredTagsCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            var Data = command.GetCommandData();
            var add = Data.GetArg("add")?.GetValue<bool>()??true;
            var value = Data.GetArg("tags")?.GetValue<string?>()??"";
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
}
