using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Data;
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

        public static void CreateArchipelagoHandlers(this ArchipelagoSession session, DiscordBot discordBot, CommandData.CommandDataModel Data)
        {
            session.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            session.Socket.SocketClosed += async (reason) =>
            {
                if (!discordBot.ActiveSessions.ContainsKey(Data.channelId)) { return; } //Bot was disconnected already
                await CleanAndCloseChannel(discordBot, Data.channelId);
                discordBot.QueueMessage(Data.socketTextChannel!, $"Connection closed:\n{reason}");
            };
            void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, discordBot)) { return; }
                StringBuilder FormattedMessage = new StringBuilder();
                StringBuilder RawMessage = new StringBuilder();

                foreach (var part in message.Parts)
                {
                    FormattedMessage.Append(part.Text.SetColor(part.Color));
                    RawMessage.Append(part.Text);
                }
                if (string.IsNullOrWhiteSpace(FormattedMessage.ToString())) { return; }

                Console.WriteLine($"Queueing message from AP session " +
                    $"{session.Socket.Uri} {session.Players.GetPlayerName(session.ConnectionInfo.Slot)} {session.ConnectionInfo.Game}");

                MessageQueueData.QueuedMessage queuedMessage = new MessageQueueData.QueuedMessage()
                {
                    Channel = Data.socketTextChannel,
                    Message = FormattedMessage.ToString(),
                    RawMessage = RawMessage.ToString(),
                    UserToPing = null
                };
                MessageQueueData.QueueMessage(queuedMessage, discordBot);
            }
        }
    }
}
