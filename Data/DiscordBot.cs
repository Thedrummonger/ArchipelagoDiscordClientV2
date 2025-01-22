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
        public class DiscordBot
        {
            public Dictionary<ulong, ActiveBotSession> ActiveSessions = [];
            public Dictionary<ulong, SessionConstructor> ConnectionCache = [];
            public AppSettings appSettings;
            public CommandRegistry commandRegistry;
            public SlashCommandHandlers CommandHandler;
            public DiscordMessageHandlers DiscordMessageHandler;
            public BotAPIRequestQueue DiscordAPIQueue;

            public DiscordBot(AppSettings Settings)
            {
                appSettings = Settings;
                CommandHandler = new SlashCommandHandlers(this);
                commandRegistry = new CommandRegistry(this);
                DiscordMessageHandler = new DiscordMessageHandlers(this);
                DiscordAPIQueue = new BotAPIRequestQueue();
                Client = new DiscordSocketClient(DiscordSocketConfig);
            }

            public DiscordSocketClient Client { get; private set; }
            public async Task Start()
            {
                await Client!.LoginAsync(TokenType.Bot, appSettings.BotToken.Trim());
                await Client.StartAsync();
            }
            public void UpdateConnectionCache(ulong ChannelID, SessionConstructor NewCachedSession)
            {
                ConnectionCache[ChannelID] = NewCachedSession;
                UpdateConnectionCache();
            }
            public void UpdateConnectionCache(ulong ChannelID, SessionSetting UpdatedSettings)
            {
                ConnectionCache[ChannelID].Settings = UpdatedSettings;
                UpdateConnectionCache();
            }
            public void UpdateConnectionCache()
            {
                File.WriteAllText(Constants.Paths.ConnectionCache, ConnectionCache.ToFormattedJson());
            }
        }

        public static readonly DiscordSocketConfig DiscordSocketConfig = new()
        {
            LogLevel = Debugger.IsAttached ? LogSeverity.Debug : LogSeverity.Info,
            GatewayIntents = GatewayIntents.GuildMessages |
                            GatewayIntents.DirectMessages |
                            GatewayIntents.Guilds |
                            GatewayIntents.MessageContent |
                            GatewayIntents.GuildIntegrations
        };
    }
}
