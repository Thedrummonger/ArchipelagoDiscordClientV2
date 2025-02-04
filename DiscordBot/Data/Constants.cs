namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Constants
    {
        public static readonly Version APVersion = new Version(0, 5, 1);

        public static class Paths
        {
            /// <summary>
            /// The base directory for storing configuration files.
            /// </summary>
            public static readonly string BaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
            /// <summary>
            /// The file path for storing bot configuration settings.
            /// </summary>
            public static readonly string ConfigFile = Path.Combine(BaseFilePath, "Config.json");
            /// <summary>
            /// The file path for storing cached Archipelago connection data.
            /// </summary>
            public static readonly string ConnectionCache = Path.Combine(BaseFilePath, "ConnectionCache.json");
        }

        /// <summary>
        /// Defines Discord rate limits and API constraints.
        /// </summary>
        public static class DiscordRateLimits
        {
            // Reference: https://javacord.org/wiki/advanced-topics/ratelimits.html

            //Currently with discord.net, it seems that rate limits lower than 1 per second are rounded up to 1 per second
            //So any value less that 1000 (except the global api call limit?) triggers a preemptive rate limit for more than one a second
            //This only really matters for send message, since that's a majority of what this bot does.

            /// <summary>
            /// The delay (in milliseconds) enforced between sending messages to prevent rate limiting.
            /// </summary>
            public static readonly int SendMessage = 1000;

            /// <summary>
            /// The delay (in milliseconds) before a message can be deleted.
            /// </summary>
            public static readonly int DeleteMessage = 200;

            /// <summary>
            /// The delay (in milliseconds) before a reaction can be edited.
            /// </summary>
            public static readonly int EditReaction = 250;

            /// <summary>
            /// The delay (in milliseconds) before editing server member information.
            /// </summary>
            public static readonly int EditServerMembers = 1000;

            /// <summary>
            /// The delay (in milliseconds) before changing a user's nickname.
            /// </summary>
            public static readonly int EditNickName = 1000;

            /// <summary>
            /// The delay (in milliseconds) before updating the bot's username.
            /// </summary>
            public static readonly int EditBotUsername = 1800000;

            /// <summary>
            /// The delay (in milliseconds) before updating a Discord channel.
            /// </summary>
            public static readonly int UpdateChannel = 300000;

            /// <summary>
            /// The minimum delay (in milliseconds) between API calls.
            /// </summary>
            public static readonly int APICalls = 20;

            /// <summary>
            /// The delay (in milliseconds) when idle, before checking for new messages or actions.
            /// </summary>
            public static readonly int IdleDelay = 100;

            /// <summary>
            /// The maximum character limit for a standard Discord message.
            /// </summary>
            public static readonly int DiscordMessageLimit = 2000;

            /// <summary>
            /// The maximum character limit for a single Discord embed message.
            /// </summary>
            public static readonly int DiscordEmbedMessageLimit = 4000;

            /// <summary>
            /// The total character limit for an embedded message, including all fields and descriptions.
            /// </summary>
            public static readonly int DiscordEmbedTotalLimit = 6000;

        }
    }
}
