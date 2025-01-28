namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Constants
    {
        public static readonly Version APVersion = new Version(0, 5, 1);

        public static class Paths
        {
            public static readonly string BaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
            public static readonly string ConfigFile = Path.Combine(BaseFilePath, "Config.json");
            public static readonly string ConnectionCache = Path.Combine(BaseFilePath, "ConnectionCache.json");
        }

        public static class DiscordRateLimits
        {
            //https://javacord.org/wiki/advanced-topics/ratelimits.html

            //Currently with discord.net, it seems that rate limits lower than 1 per second are rounded up to 1 per second
            //So any value less that 1000 (except the global api call limit?) triggers a preemptive rate limit for more than one a second
            //THis only really matters for send message, since that's a majority of what this bot does.
            public static readonly int SendMessage = 1000;
            public static readonly int DeleteMessage = 200;
            public static readonly int EditReaction = 250;
            public static readonly int EditServerMembers = 1000;
            public static readonly int EditNickName = 1000;
            public static readonly int EditBotUsername = 1800000;
            public static readonly int UpdateChannel = 300000;
            public static readonly int APICalls = 20;

            public static readonly int IdleDelay = 100;

            public static readonly int DiscordMessageLimit = 2000;
            public static readonly int DiscordEmbedMessageLimit = 4000;
            public static readonly int DiscordEmbedTotalLimit = 6000;

        }
    }
}
