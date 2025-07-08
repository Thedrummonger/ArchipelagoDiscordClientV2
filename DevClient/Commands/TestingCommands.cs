using ArchipelagoDiscordClientLegacy.Helpers;
using Discord.WebSocket;
using Discord;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using ArchipelagoDiscordClientLegacy.Commands;

namespace DevClient.Commands
{
    class TestingCommands
    {
        public class ShowColorsCommand : ICommand
        {
            public string Name => "show_colors";

            public SlashCommandProperties Properties =>
                new SlashCommandBuilder()
                    .WithName(Name)
                    .WithDescription("Display all configured ANSI colors and their Archipelago usages")
                    .Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                // Build the ANSI‐wrapped showcase string
                var demo = BuildColorShowcase();
                await command.RespondAsync(demo);
            }

            private static readonly Tuple<string, string> Formatter = new("```ansi\n", "\n```");
            private const string LineSeparator = "\n";

            private static string BuildColorShowcase()
            {
                var demoItems = new (Archipelago.MultiClient.Net.Models.Color Color, string Name, string Usage)[]
                {
                    (Archipelago.MultiClient.Net.Models.Color.Red,       "Red",       "Hints.Unfound"),
                    (Archipelago.MultiClient.Net.Models.Color.Green,     "Green",     "Hints.Found / Locations.Location"),
                    (Archipelago.MultiClient.Net.Models.Color.Yellow,    "Yellow",    "Players.Other"),
                    (Archipelago.MultiClient.Net.Models.Color.Magenta,   "Magenta",   "Players.Local"),
                    (Archipelago.MultiClient.Net.Models.Color.Blue,      "Blue",      "Locations.Entrance"),
                    (Archipelago.MultiClient.Net.Models.Color.Cyan,      "Cyan",      "Items.Normal"),
                    (Archipelago.MultiClient.Net.Models.Color.SlateBlue, "SlateBlue", "Items.Important"),
                    (Archipelago.MultiClient.Net.Models.Color.Salmon,    "Salmon",    "Items.Traps"),
                    (Archipelago.MultiClient.Net.Models.Color.Plum,      "Plum",      "Items.Progression"),
                    (Archipelago.MultiClient.Net.Models.Color.Black,     "Black",     "(unused)"),
                };

                var lines = demoItems.Select(item =>
                    $"{item.Name.SetColor(item.Color)} — {item.Usage}"
                );

                return $"{Formatter.Item1}{string.Join(LineSeparator, lines)}{Formatter.Item2}";
            }
        }
    }
}
