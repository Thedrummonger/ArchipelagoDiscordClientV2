﻿namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Constants
    {
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

        public static class Paths
        {
            public static readonly string BaseFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DrathBot", "Archipelago");
            public static readonly string ConfigFile = Path.Combine(BaseFilePath, "Config.json");
        }
    }
}
