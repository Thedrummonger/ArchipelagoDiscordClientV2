using ArchipelagoDiscordClientLegacy.Commands;
using ArchipelagoDiscordClientLegacy.Handlers;
using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public class DiscordBotData
    {
        /// <summary>
        /// Represents the core Discord bot instance, managing active sessions, command handling, and API interactions.
        /// </summary>
        public class DiscordBot
        {
            public Dictionary<ulong, ActiveBotSession> ActiveSessions = [];
            public Dictionary<ulong, SessionConstructor> ConnectionCache = [];
            public AppSettings appSettings;
            public CommandRegistry commandRegistry;
            public SlashCommandHandlers CommandHandler;
            public DiscordMessageHandlers DiscordMessageHandler;
            public BotAPIRequestQueue DiscordAPIQueue;
            /// <summary>
            /// Initializes a new instance of the <see cref="DiscordBot"/> class.
            /// </summary>
            /// <param name="Settings">The application settings used for bot configuration.</param>
            public DiscordBot(AppSettings Settings)
            {
                appSettings = Settings;
                CommandHandler = new SlashCommandHandlers(this);
                commandRegistry = new CommandRegistry(this);
                DiscordMessageHandler = new DiscordMessageHandlers(this);
                DiscordAPIQueue = new BotAPIRequestQueue(this);
                Client = new DiscordSocketClient(BotSocketConfig.GetConfig());
            }

            public DiscordSocketClient Client { get; private set; }
            /// <summary>
            /// Starts the Discord bot by logging in and connecting to Discord.
            /// </summary>
            public async Task Start()
            {
                await Client!.LoginAsync(TokenType.Bot, appSettings.BotToken.Trim());
                await Client.StartAsync();
            }
            /// <summary>
            /// Creates cached session data for the given channel. Should be used when a new session is created.
            /// </summary>
            /// <param name="ChannelID">The Discord channel ID associated with the session.</param>
            /// <param name="NewCachedSession">The new session data to store in the cache.</param>
            public void UpdateConnectionCache(ulong ChannelID, SessionConstructor NewCachedSession)
            {
                ConnectionCache[ChannelID] = NewCachedSession;
                UpdateConnectionCache();
            }
            /// <summary>
            /// Updates the cached session data for active session for the given channel.
            /// </summary>
            /// <param name="ChannelID">The Discord channel ID associated with the active session.</param>
            public void UpdateConnectionCache(ulong ChannelID)
            {
                var ActiveSession = ActiveSessions[ChannelID];
                var CachedSession = ConnectionCache[ChannelID];
                CachedSession.Settings = ActiveSession.Settings;
                CachedSession.AuxiliarySessions = [.. ActiveSession.AuxiliarySessions.Keys];
                UpdateConnectionCache();
            }
            private void UpdateConnectionCache() => File.WriteAllText(Constants.Paths.ConnectionCache, ConnectionCache.ToFormattedJson());
        }
        /// <summary>
        /// Configures the Discord bot's settings, including logging levels and gateway intents.
        /// </summary>
        public static class BotSocketConfig
        {
            private static LogSeverity _LogSeverity = LogSeverity.Info;
            public static void SetLogLevel(LogSeverity logLevel) => _LogSeverity = logLevel;

            public static DiscordSocketConfig GetConfig() => new()
            {
                LogLevel = _LogSeverity,
                GatewayIntents = GatewayIntents.GuildMessages |
                                GatewayIntents.DirectMessages |
                                GatewayIntents.Guilds |
                                GatewayIntents.MessageContent |
                                GatewayIntents.GuildIntegrations
            };
        }
    }
}
