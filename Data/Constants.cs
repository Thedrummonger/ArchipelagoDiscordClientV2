namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Constants
    {
        public class Colors
        {
            public class Hints
            {
                public static readonly Archipelago.MultiClient.Net.Models.Color Unfound = Archipelago.MultiClient.Net.Models.Color.Red;
                public static readonly Archipelago.MultiClient.Net.Models.Color found = Archipelago.MultiClient.Net.Models.Color.Green;
            }
            public class Players
            {
                //NOTE these are backwards in the comments in the library
                public static readonly Archipelago.MultiClient.Net.Models.Color Local = Archipelago.MultiClient.Net.Models.Color.Magenta;
                public static readonly Archipelago.MultiClient.Net.Models.Color Other = Archipelago.MultiClient.Net.Models.Color.Yellow;
            }
            public class Items
            {
                public static readonly Archipelago.MultiClient.Net.Models.Color Normal = Archipelago.MultiClient.Net.Models.Color.Cyan;
                public static readonly Archipelago.MultiClient.Net.Models.Color Important = Archipelago.MultiClient.Net.Models.Color.SlateBlue;
                public static readonly Archipelago.MultiClient.Net.Models.Color Traps = Archipelago.MultiClient.Net.Models.Color.Salmon;
                public static readonly Archipelago.MultiClient.Net.Models.Color Progression = Archipelago.MultiClient.Net.Models.Color.Plum;
            }
            public static readonly Archipelago.MultiClient.Net.Models.Color Entrance = Archipelago.MultiClient.Net.Models.Color.Blue;
            public static readonly Archipelago.MultiClient.Net.Models.Color Location = Archipelago.MultiClient.Net.Models.Color.Green;


            public static readonly Dictionary<Archipelago.MultiClient.Net.Models.Color, Tuple<string, string>> ColorCodes = new()
            {
                { Archipelago.MultiClient.Net.Models.Color.Red, new (@"[2;31m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Green, new (@"[2;32m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Yellow, new (@"[2;33m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Blue, new (@"[2;34m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Magenta, new (@"[2;35m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Cyan, new (@"[2;36m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Black, new (@"[2;30m", @"[0m") },
                //{ Archipelago.MultiClient.Net.Models.Color.White, new (@"[2;37m", @"[0m") }, //Uncolored discord messages are already white
                { Archipelago.MultiClient.Net.Models.Color.SlateBlue, new (@"[2;34m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Salmon, new (@"[2;33m", @"[0m") },
                { Archipelago.MultiClient.Net.Models.Color.Plum, new (@"[2;35m", @"[0m") }
            };
        }

        public static class Paths
        {
            public static readonly string BaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
            public static readonly string ConfigFile = Path.Combine(BaseFilePath, "Config.json");
            public static readonly string ConnectionCache = Path.Combine(BaseFilePath, "ConnectionCache.json");
        }
    }
}
