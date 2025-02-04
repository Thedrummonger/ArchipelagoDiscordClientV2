using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class CommandHelpers
    {
        /// <summary>
        /// Validates whether the specified channel is connected or disconnected to an AP server.
        /// </summary>
        /// <param name="command">The Discord slash command context.</param>
        /// <param name="discordBot">The Discord bot managing active sessions.</param>
        /// <param name="isConnected">True to check if the channel is connected, false to check if disconnected.</param>
        /// <param name="commandData">Extracted command data.</param>
        /// <param name="errorMessage">Error message if validation fails.</param>
        /// <returns>True if validation succeeds, otherwise false.</returns>
        public static bool Validate(
            this SocketSlashCommand command,
            DiscordBot discordBot,
            bool isConnected,
            out CommandData.CommandDataModel commandData,
            out string errorMessage
            )
        {
            errorMessage = "Unknown Error";
            commandData = command.GetCommandData();
            if (commandData.TextChannel is null)
            {
                Console.WriteLine($"Tried to connect with channel type {command.Channel.GetType()}");
                errorMessage = "Only Text Channels are Supported";
                return false;
            }

            if (discordBot.ActiveSessions.ContainsKey(commandData.ChannelId) != isConnected)
            {
                errorMessage = isConnected ?
                    "This channel is not connected to an Archipelago session." :
                    "This channel is already connected to an Archipelago session.";
                return false;
            }
            return true;
        }
        /// <summary>
        /// Validates that the specified channel is connected to an AP server and retrieves the active session.
        /// </summary>
        /// <param name="command">The Discord slash command context.</param>
        /// <param name="discordBot">The Discord bot managing active sessions.</param>
        /// <param name="session">The active bot session if the channel is connected.</param>
        /// <param name="commandData">Extracted command data.</param>
        /// <param name="errorMessage">Error message if validation fails.</param>
        /// <returns>True if validation succeeds and a session is found, otherwise false.</returns>
        public static bool Validate(
            this SocketSlashCommand command,
            DiscordBot discordBot,
            out Sessions.ActiveBotSession? session,
            out CommandData.CommandDataModel commandData,
            out string errorMessage
        )
        {
            session = null;

            if (!Validate(command, discordBot, true, out commandData, out errorMessage))
                return false;

            return discordBot.ActiveSessions.TryGetValue(commandData.ChannelId, out session); // Should never be false
        }

        /// <summary>
        /// Creates a formatted result list by prepending a header message to a collection of values.
        /// </summary>
        /// <param name="Values">The collection of string values to format.</param>
        /// <param name="Header">The header message to include as the first element.</param>
        /// <returns>
        /// An array where the first element is the header message, followed by each value prefixed with a hyphen.
        /// Returns an empty array if <paramref name="Values"/> is empty.
        /// </returns>
        public static string[] CreateResultList(this IEnumerable<string> Values, string Header)
        {
            if (!Values.Any()) return [];
            return [Header, .. Values.Select(x => $"-{x}")];
        }

        /// <summary>
        /// Creates a Discord embed from a list of values with a specified header and optional color.
        /// </summary>
        /// <param name="Values">A collection of strings representing the values to display in the embed.</param>
        /// <param name="Header">The title/header of the embed.</param>
        /// <param name="color">An optional <see cref="Color"/> for the embed. If not provided, the default embed color is used.</param>
        /// <returns>An <see cref="Embed"/> containing the provided values.</returns>
        public static Embed CreateEmbedResultsList(this IEnumerable<string> Values, string Header, Color? color = null)
        {
            var builder = new EmbedBuilder().WithTitle(Header).WithDescription(string.Join("\n", Values));
            if (color is Color c) builder.WithColor(c);
            return builder.Build();
        }

        /// <summary>
        /// Creates a formatted Discord embed with a title, description, color, and multiple optional fields.
        /// </summary>
        /// <param name="title">The title of the embed. If null or empty, no title is set.</param>
        /// <param name="description">The main description of the embed. If null or empty, no description is set.</param>
        /// <param name="color">An optional <see cref="Color"/> for the embed. If not provided, the default embed color is used.</param>
        /// <param name="fields">An array of named fields where each field contains a name and a collection of values.</param>
        /// <returns>An <see cref="EmbedBuilder"/> containing the specified content.</returns>
        public static EmbedBuilder CreateCommandResultEmbed(string? title, string? description, Color? color, params (string name, IEnumerable<string> values)[] fields)
        {
            var embed = new EmbedBuilder().WithCurrentTimestamp();
            if (!string.IsNullOrWhiteSpace(title)) embed.WithTitle(title);
            if (!string.IsNullOrWhiteSpace(description)) embed.WithDescription(description);
            if (color is Color c) embed.WithColor(c);

            foreach (var (name, values) in fields) 
            { 
                if (values.Any())
                {
                    embed.AddField(name, string.Join("\n", values), false);
                }
            }
            return embed;
        }

        public enum AddRemoveAction
        {
            add,
            remove,
        }
        public static readonly string AddRemoveActionName = "action";
        /// <summary>
        /// Adds a required slash command option named "action" with "add" and "remove" choices.
        /// </summary>
        /// <param name="commandBuilder">The slash command builder to modify.</param>
        /// <returns>The updated <see cref="SlashCommandBuilder"/> instance.</returns>
        public static SlashCommandBuilder AddRemoveActionOption(this SlashCommandBuilder commandBuilder)
        {
            var optionBuilder = new SlashCommandOptionBuilder()
                .WithName(AddRemoveActionName) // Set the option name to "action"
                .WithDescription("Select a setting to toggle") // Provide a user-friendly description
                .WithRequired(true) // Make the option mandatory
                .WithType(ApplicationCommandOptionType.Integer) // Use an integer type for compatibility
                .AddChoice(AddRemoveAction.add.ToString(), (int)AddRemoveAction.add) // Add "add" choice
                .AddChoice(AddRemoveAction.remove.ToString(), (int)AddRemoveAction.remove); // Add "remove" choice

            commandBuilder.AddOption(optionBuilder); // Attach the option to the command
            return commandBuilder;
        }
    }
}
