using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    /// <summary>
    /// Handles messages received in Discord and relays them to the Archipelago server if applicable.
    /// </summary>
    public class DiscordMessageHandlers(DiscordBotData.DiscordBot discordBot)
    {
        /// <summary>
        /// Processes an incoming Discord message and determines if it should be relayed to an Archipelago session.
        /// </summary>
        /// <param name="message">The Discord message received.</param>
        public async Task HandleDiscordMessageReceivedAsync(SocketMessage message)
        {
            // Ignore messages from bots
            if (message.Author.IsBot) return;

            // Check if the message was sent in a guild text channel
            if (message.Channel is not ISocketMessageChannel textChannel) { return; }

            var channelId = textChannel.Id;

            // Check if the message was sent in an active Archipelago session channel
            if (discordBot.ActiveSessions.TryGetValue(channelId, out Sessions.ActiveBotSession? Session))
            {
                await RelayMessageToArchipelago(message, Session);
            }
        }
        /// <summary>
        /// Relays a Discord message to the Archipelago server as an in-game chat message.
        /// </summary>
        /// <remarks>
        /// Archipelago broadcasts chat messages to all connected clients, including the sender. 
        /// This may cause duplicated messages in Discord since there is no direct way to track 
        /// and prevent reposting messages that originated from the same channel.
        /// </remarks>
        /// <param name="message">The Discord message to relay.</param>
        /// <param name="activeBotSession">The active Archipelago session for the Discord channel.</param>
        public static async Task RelayMessageToArchipelago(SocketMessage message, Sessions.ActiveBotSession activeBotSession)
        {
            if (string.IsNullOrWhiteSpace(message.Content)) { return; }

            string Message = $"[Discord: {message.Author.Username}] {message.Content}]";
            try
            {
                await activeBotSession.ArchipelagoSession.Socket.SendPacketAsync(new SayPacket() { Text = Message });
                Console.WriteLine($"Message sent to Archipelago from {message.Author.Username} in {message.Channel.Name}: {message.Content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
                activeBotSession.QueueMessageForChannel($"Error: Unable to send message to the Archipelago server.\n{ex.Message}");
            }
        }
    }
}
