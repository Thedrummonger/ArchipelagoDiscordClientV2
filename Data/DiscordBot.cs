using ArchipelagoDiscordClientLegacy.Commands;
using ArchipelagoDiscordClientLegacy.Handlers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public class DiscordBotData
    {
        public class DiscordBot
        {
            public bool BotIsLive = false;
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

            private DiscordSocketClient? Client;
            public DiscordSocketClient GetClient()
            {
                if (Client == null) { throw new InvalidOperationException("Can't get client before Client is Initialized"); }
                return Client;
            }
            public async Task Start()
            {
                await Client!.LoginAsync(TokenType.Bot, appSettings.BotToken.Trim());
                await Client.StartAsync();
            }
            public void UpdateConnectionCache()
            {
                File.WriteAllText(Constants.Paths.ConnectionCache, ConnectionCache.ToFormattedJson());
            }
        }

        public static readonly DiscordSocketConfig DiscordSocketConfig = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.All //I'm to lazy to figure out exactly what intents are needed, gimme all
        };
    }
}
