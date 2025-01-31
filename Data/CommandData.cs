using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class CommandData
    {
        /// <summary>
        /// Represents structured data extracted from a Discord slash command.
        /// </summary>
        public class CommandDataModel
        {
            public required ulong GuildId { get; set; }
            public required ulong ChannelId { get; set; }
            public string? ChannelName { get; set; }
            public ISocketMessageChannel? TextChannel { get; set; }
            public Dictionary<string, SocketSlashCommandDataOption> Arguments { get; set; } = [];
            public SocketSlashCommandDataOption? GetArg(string key)
            {
                if (!Arguments.TryGetValue(key, out var arg)) { return null; }
                return arg;
            }
        }
        /// <summary>
        /// Extracts structured data from a Discord slash command and returns it as a <see cref="CommandDataModel"/>.
        /// </summary>
        /// <param name="command">The incoming slash command.</param>
        /// <returns>A <see cref="CommandDataModel"/> containing structured command data.</returns>
        public static CommandDataModel GetCommandData(this SocketSlashCommand command)
        {
            var Data = new CommandDataModel()
            {
                GuildId = command.GuildId ?? 0,
                ChannelId = command.ChannelId ?? 0,
                ChannelName = command.Channel?.Name,
                TextChannel = command.Channel is ISocketMessageChannel STC ? STC : null
            };
            Data.ChannelName ??= Data.ChannelId.ToString();
            foreach (var i in command.Data.Options)
            {
                Data.Arguments[i.Name] = i;
            }
            return Data;
        }
        /// <summary>
        /// Attempts to retrieve the value of a command option and convert it to the specified type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="option">The command option containing the value.</param>
        /// <returns>The converted value if successful, otherwise the default value of <typeparamref name="T"/>.</returns>

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
