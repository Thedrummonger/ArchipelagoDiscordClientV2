using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class CommandData
    {
        public class CommandDataModel
        {
            public required ulong guildId { get; set; }
            public required ulong channelId { get; set; }
            public string? channelName { get; set; }
            public ISocketMessageChannel? textChannel { get; set; }
            public Dictionary<string, SocketSlashCommandDataOption> Arguments { get; set; } = [];
            public SocketSlashCommandDataOption? GetArg(string key)
            {
                if (!Arguments.TryGetValue(key, out var arg)) { return null; }
                return arg;
            }
        }
        public static CommandDataModel GetCommandData(this SocketSlashCommand command)
        {
            var Data = new CommandDataModel()
            {
                guildId = command.GuildId ?? 0,
                channelId = command.ChannelId ?? 0,
                channelName = command.Channel?.Name,
                textChannel = command.Channel is ISocketMessageChannel STC ? STC : null
            };
            Data.channelName ??= Data.channelId.ToString();
            foreach (var i in command.Data.Options)
            {
                Data.Arguments[i.Name] = i;
            }
            return Data;
        }
        public static T? GetValue<T>(this SocketSlashCommandDataOption option)
        {
            object value = option.Value;
            if (value == null)
                return default;
            if (value is T tValue)
                return tValue;
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
    }
}
