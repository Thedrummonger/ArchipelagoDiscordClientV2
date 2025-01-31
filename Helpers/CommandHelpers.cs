using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
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
