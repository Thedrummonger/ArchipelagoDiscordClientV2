﻿using Archipelago.MultiClient.Net.Packets;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class DiscordMessageHandlers(DiscordBotData.DiscordBot discordBot)
    {
        public async Task HandleDiscordMessageReceivedAsync(SocketMessage message)
        {
            // Ignore messages from bots
            if (message.Author.IsBot) return;

            // Check if the message was sent in a guild text channel
            if (message.Channel is not SocketTextChannel textChannel) { return; }

            var guildId = textChannel.Guild.Id;
            var channelId = textChannel.Id;

            if (discordBot.ActiveSessions.ContainsKey(channelId))
            {
                RelayMessageToArchipelago(message, discordBot.ActiveSessions[channelId], discordBot);
            }
        }

        // TODO: Due to the way Archipelago handles "chat" messages, any message sent to AP 
        // is broadcast to all clients, including the one that originally sent it. 
        // This results in messages being duplicated in the Discord chat.
        // Ideally, I want to avoid posting a message to Discord if it originated 
        // from the same channel, but I can't think of a good way to track that. 
        public static async void RelayMessageToArchipelago(SocketMessage message, Sessions.ActiveBotSession activeBotSession, DiscordBotData.DiscordBot discordBot)
        {
            if (string.IsNullOrWhiteSpace(message.Content)) { return; }
            string Message = $"[Discord: {message.Author.Username}] {message.Content}";
            try
            {
                // Send the message to the Archipelago server
                await activeBotSession.archipelagoSession.Socket.SendPacketAsync(new SayPacket() { Text = Message });
                Console.WriteLine($"Message sent to Archipelago from {message.Author.Username} in {message.Channel.Name}: {message.Content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to Archipelago: {ex.Message}");
                discordBot.QueueSimpleMessage(activeBotSession.DiscordChannel, $"Error: Unable to send message to the Archipelago server.\n{ex.Message}");
                //await textChannel.SendMessageAsync("Error: Unable to send message to the Archipelago server.");
            }
        }
    }
}
