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
    }
}
