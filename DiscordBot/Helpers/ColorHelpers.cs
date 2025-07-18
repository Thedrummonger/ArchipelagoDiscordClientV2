﻿using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    /// <summary>
    /// Provides helper methods and predefined color mappings for formatting text with color in Discord messages.
    /// </summary>
    /// <remarks>
    /// Uses Discord's code markdown for text coloring. The ANSI escape sequences approximate 
    /// Archipelago's colors but may not match exactly. Reference: 
    /// https://gist.github.com/kkrypt0nn/a02506f3712ff2d1c8ca7c9e0aed7c06
    /// </remarks>
    public static class ColorHelpers
    {

        /// <summary>
        /// Wraps a string with the appropriate ANSI escape sequences to apply color formatting.
        /// </summary>
        /// <param name="input">The text to format.</param>
        /// <param name="color">The color to apply, based on Archipelago's color model.</param>
        /// <returns>The formatted string with ANSI color codes if a matching color exists, otherwise the original string.</returns>
        public static string SetColor(this string input, Archipelago.MultiClient.Net.Models.Color color) =>
            ColorCodes.TryGetValue(color, out Tuple<string, string>? Parts) ? $"{Parts.Item1}{input}{Parts.Item2}" : input;
        /// <summary>
        /// Determines the appropriate Discord embed color based on the number of successes and failures in the given collections.
        /// </summary>
        /// <param name="successes">An <see cref="IEnumerable{object}"/> containing successful operations.</param>
        /// <param name="failures">An <see cref="IEnumerable{object}"/> containing failed operations.</param>
        /// <returns>
        /// <see cref="Discord.Color.Green"/> if there are no failures,
        /// <see cref="Discord.Color.Orange"/> if there are both successes and failures,
        /// and <see cref="Discord.Color.Red"/> if there are only failures.
        /// </returns>
        public static Discord.Color GetResultEmbedStatusColor(IEnumerable<object> successes, IEnumerable<object> failures) =>
            GetResultEmbedStatusColor(successes.Count(), failures.Count());
        /// <summary>
        /// Determines the appropriate Discord embed color based on success and failure counts.
        /// </summary>
        /// <param name="successCount">The number of successful operations.</param>
        /// <param name="failureCount">The number of failed operations.</param>
        /// <returns>
        /// <see cref="Discord.Color.Green"/> if there are no failures,
        /// <see cref="Discord.Color.Orange"/> if there are both successes and failures,
        /// and <see cref="Discord.Color.Red"/> if there are only failures.
        /// </returns>
        public static Discord.Color GetResultEmbedStatusColor(int successCount, int failureCount) =>
            failureCount == 0 ? Discord.Color.Green : successCount > 0 ? Discord.Color.Orange : Discord.Color.Red;

        public static string GetColoredString(this ItemInfo item) => GetColoredString(item.ItemName, item.Flags);
        public static string GetColoredString(string ItemName, ItemFlags flags)
        {
            var Final = ItemName;
            if (flags.HasFlag(ItemFlags.Advancement)) { Final = Final.SetColor(ColorHelpers.Items.Progression); }
            else if (flags.HasFlag(ItemFlags.NeverExclude)) { Final = Final.SetColor(ColorHelpers.Items.Important); }
            else if (flags.HasFlag(ItemFlags.Trap)) { Final = Final.SetColor(ColorHelpers.Items.Traps); }
            else { Final = Final.SetColor(ColorHelpers.Items.Normal); }
            return Final;
        }
        public static string GetColoredString(this PlayerInfo Player, string ActivePlayerName)
        {
            string PlayerString = Player.Name;
            return Player.Name == ActivePlayerName ? PlayerString.SetColor(ColorHelpers.Players.Local) : PlayerString.SetColor(ColorHelpers.Players.Other);
        }
        public static string GetColoredString(this Hint hint)
        {
            var FoundColor = hint.Status switch
            {
                HintStatus.Found => ColorHelpers.Hints.found,
                HintStatus.Priority => ColorHelpers.Items.Progression,
                HintStatus.Avoid => ColorHelpers.Items.Traps,
                HintStatus.NoPriority => ColorHelpers.Items.Normal,
                _ => Archipelago.MultiClient.Net.Models.Color.White,
            };
            return ColorHelpers.SetColor(hint.Status.ToString(), FoundColor);

        }

        /// <summary>
        /// Predefined color mappings for hint-related messages.
        /// </summary>
        public class Hints
        {
            public static readonly Archipelago.MultiClient.Net.Models.Color Unfound = Archipelago.MultiClient.Net.Models.Color.Red;
            public static readonly Archipelago.MultiClient.Net.Models.Color found = Archipelago.MultiClient.Net.Models.Color.Green;
        }
        /// <summary>
        /// Predefined color mappings for player-related messages.
        /// </summary>
        public class Players
        {
            //NOTE these are backwards in the comments in the library
            public static readonly Archipelago.MultiClient.Net.Models.Color Local = Archipelago.MultiClient.Net.Models.Color.Magenta;
            public static readonly Archipelago.MultiClient.Net.Models.Color Other = Archipelago.MultiClient.Net.Models.Color.Yellow;
        }
        /// <summary>
        /// Predefined color mappings for item-related messages.
        /// </summary>
        public class Items
        {
            public static readonly Archipelago.MultiClient.Net.Models.Color Normal = Archipelago.MultiClient.Net.Models.Color.Cyan;
            public static readonly Archipelago.MultiClient.Net.Models.Color Important = Archipelago.MultiClient.Net.Models.Color.SlateBlue;
            public static readonly Archipelago.MultiClient.Net.Models.Color Traps = Archipelago.MultiClient.Net.Models.Color.Salmon;
            public static readonly Archipelago.MultiClient.Net.Models.Color Progression = Archipelago.MultiClient.Net.Models.Color.Plum;
        }
        /// <summary>
        /// Predefined color mappings for location-related message types.
        /// </summary>
        public class Locations
        {
            public static readonly Archipelago.MultiClient.Net.Models.Color Entrance = Archipelago.MultiClient.Net.Models.Color.Blue;
            public static readonly Archipelago.MultiClient.Net.Models.Color Location = Archipelago.MultiClient.Net.Models.Color.Green;
        }

        /// <summary>
        /// Dictionary mapping Archipelago colors to their respective ANSI escape sequences for text formatting.
        /// </summary>
        /*
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
        */
        public static readonly Dictionary<Archipelago.MultiClient.Net.Models.Color, Tuple<string, string>> ColorCodes = new()
        {
			// — Hints
			{ Archipelago.MultiClient.Net.Models.Color.Red,     new("\u001b[2;31m", "\u001b[0m") },  // unfound (dim red)
			{ Archipelago.MultiClient.Net.Models.Color.Green,   new("\u001b[2;32m", "\u001b[0m") },  // found / Locations.Location (dim green)

			// — Players
			{ Archipelago.MultiClient.Net.Models.Color.Yellow,  new("\u001b[2;33m", "\u001b[0m") },  // Other (dim yellow)
			{ Archipelago.MultiClient.Net.Models.Color.Magenta, new("\u001b[2;35m", "\u001b[0m") },  // Local  (dim magenta)

			// — Locations
			{ Archipelago.MultiClient.Net.Models.Color.Blue,    new("\u001b[2;34m", "\u001b[0m") },  // Entrance (dim blue)

			// — Items
			{ Archipelago.MultiClient.Net.Models.Color.Cyan,      new("\u001b[1;36m", "\u001b[0m") },  // Normal      (bright cyan)
			{ Archipelago.MultiClient.Net.Models.Color.SlateBlue, new("\u001b[1;34m", "\u001b[0m") },  // Important   (bright blue)
			{ Archipelago.MultiClient.Net.Models.Color.Salmon,    new("\u001b[1;33m", "\u001b[0m") },  // Traps       (bright yellow)
			{ Archipelago.MultiClient.Net.Models.Color.Plum,      new("\u001b[1;35m", "\u001b[0m") },  // Progression (bright magenta)

			// — Other
			{ Archipelago.MultiClient.Net.Models.Color.Black,     new("\u001b[2;30m", "\u001b[0m") },  // unused / fallback
		};

    }
}
