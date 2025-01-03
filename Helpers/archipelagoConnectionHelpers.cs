using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using System.Linq;
using System.Text;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class archipelagoConnectionHelpers
    {
        public static async Task CleanAndCloseChannel(this DiscordBotData.DiscordBot bot, ulong channelId)
        {
            if (!bot.ActiveSessions.TryGetValue(channelId, out var session)) { return; }
            bot.ActiveSessions.Remove(channelId);
            bot.MessageQueueHandler.MessageQueue.Remove(channelId);
            Console.WriteLine($"Disconnecting Channel {session.DiscordChannel.Id} from server {session.archipelagoSession.ConnectionInfo.Slot}");
            if (session.archipelagoSession.Socket.Connected) { await session.archipelagoSession.Socket.DisconnectAsync(); }
        }

        public static void CreateArchipelagoHandlers(this ArchipelagoSession session, DiscordBot discordBot, Sessions.ActiveBotSession BotSession)
        {
            session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            session.Socket.SocketClosed += async (reason) =>
            {
                if (!discordBot.ActiveSessions.ContainsKey(BotSession.DiscordChannel.Id)) { return; } //Bot was disconnected already
                await CleanAndCloseChannel(discordBot, BotSession.DiscordChannel.Id);
                discordBot.QueueMessage(BotSession.DiscordChannel!, $"Connection closed:\n{reason}");
            };
            void MessageLog_OnMessageReceived(LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, BotSession)) { return; }
                StringBuilder FormattedMessage = new StringBuilder();
                StringBuilder RawMessage = new StringBuilder();

                foreach (var part in message.Parts)
                {
                    FormattedMessage.Append(part.Text.SetColor(part.Color));
                    RawMessage.Append(part.Text);
                }
                if (string.IsNullOrWhiteSpace(FormattedMessage.ToString())) { return; }

                HashSet<SocketUser> ToPing = [];

                if (message is ItemSendLogMessage itemSendMessage && 
                    itemSendMessage.Item.Flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement)
                    && itemSendMessage.Receiver.Slot != itemSendMessage.Sender.Slot)
                {
                    foreach (var i in BotSession.SlotAssociations.Where(x => x.Value.Contains(itemSendMessage.Receiver.Name)))
                    {
                        ToPing.Add(i.Key);
                        ToPing.Add(i.Key);
                    }
                }

                Console.WriteLine($"Queueing message from AP session " +
                    $"{session.Socket.Uri} {session.Players.GetPlayerName(session.ConnectionInfo.Slot)} {session.ConnectionInfo.Game}");

                MessageQueueData.QueuedMessage queuedMessage = new MessageQueueData.QueuedMessage()
                {
                    Channel = BotSession.DiscordChannel,
                    Message = FormattedMessage.ToString(),
                    RawMessage = RawMessage.ToString(),
                    UsersToPing = ToPing
                };
                MessageQueueData.QueueMessage(queuedMessage, discordBot);
            }
        }
    }
}
