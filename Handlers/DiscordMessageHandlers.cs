using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class DiscordMessageHandlers(DiscordBotData.DiscordBot discordBot)
    {
        public async Task HandleDiscordMessageReceivedAsync(SocketMessage message)
        {
            // Ignore messages from bots
            if (message.Author.IsBot) return;

            // Check if the message was sent in a guild text channel
            if (message.Channel is not ISocketMessageChannel textChannel) { return; }

            var channelId = textChannel.Id;

            if (discordBot.ActiveSessions.TryGetValue(channelId, out Sessions.ActiveBotSession? Session))
            {
                await RelayMessageToArchipelago(message, Session, discordBot);
            }
        }

        // TODO: Due to the way Archipelago handles "chat" messages, any message sent to AP 
        // is broadcast to all clients, including the one that originally sent it. 
        // This results in messages being duplicated in the Discord chat.
        // Ideally, I want to avoid posting a message to Discord if it originated 
        // from the same channel, but I can't think of a good way to track that. 
        public static async Task RelayMessageToArchipelago(SocketMessage message, Sessions.ActiveBotSession activeBotSession, DiscordBotData.DiscordBot discordBot)
        {
            if (string.IsNullOrWhiteSpace(message.Content)) { return; }

            string Message = $"[Discord: {message.Author.Username}] {message.Content}]";
            try
            {
                await activeBotSession.archipelagoSession.Socket.SendPacketAsync(new SayPacket() { Text = Message });
                Console.WriteLine($"Message sent to Archipelago from {message.Author.Username} in {message.Channel.Name}: {message.Content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
                discordBot.QueueMessage(activeBotSession.DiscordChannel, $"Error: Unable to send message to the Archipelago server.\n{ex.Message}");
            }
        }
    }
}
