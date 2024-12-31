using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class archipelagoConnectionHelpers
    {
        public static async Task CleanAndCloseChannel(this DiscordBotData.DiscordBot bot, ulong channelId)
        {
            if (!bot.ActiveSessions.TryGetValue(channelId, out var session)) { return; }
            Console.WriteLine($"Disconnecting Channel {session.DiscordChannel.Id} from server {session.archipelagoSession.ConnectionInfo.Slot}");
            if (session.archipelagoSession.Socket.Connected) { await session.archipelagoSession.Socket.DisconnectAsync(); }
            bot.ActiveSessions.Remove(channelId);
            bot.MessageQueueHandler.MessageQueue.Remove(channelId);
        }

        public static void CreateArchipelagoHandlers(this ArchipelagoSession session, DiscordBot discordBot, CommandData.CommandDataModel Data)
        {
            session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            session.Socket.SocketClosed += async (reason) =>
            {
                await CleanAndCloseChannel(discordBot, Data.channelId);
                discordBot.QueueSimpleMessage(Data.socketTextChannel!, $"Connection closed:\n{reason}");
            };
            void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, discordBot)) { return; }
                StringBuilder FormattedMessage = new StringBuilder();
                StringBuilder RawMessage = new StringBuilder();

                foreach (var part in message.Parts)
                {
                    FormattedMessage.Append(part.Text.SetColor(part.Color));
                    FormattedMessage.Append(part.Text);
                }
                if (string.IsNullOrWhiteSpace(FormattedMessage.ToString())) { return; }
                Console.WriteLine($"Queueing message from AP session " +
                    $"{session.Socket.Uri} {session.Players.GetPlayerName(session.ConnectionInfo.Slot)} {session.ConnectionInfo.Game}");

                MessageQueue.QueuedMessage queuedMessage = new MessageQueue.QueuedMessage()
                {
                    Channel = Data.socketTextChannel,
                    Message = FormattedMessage.ToString(),
                    RawMessage = RawMessage.ToString(),
                    UserToPing = null
                };
                MessageQueue.QueueMessage(queuedMessage, discordBot);
            }
        }
    }
}
