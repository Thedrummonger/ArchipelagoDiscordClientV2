using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDiscordClientLegacy.Data;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class archipelagoConnectionHelpers
    {
        public static async Task CleanAndCloseChannel(this DiscordBot bot, ulong channelId)
        {
            if (!bot.ActiveSessions.TryGetValue(channelId, out var session)) { return; }
            bot.ActiveSessions.Remove(channelId);
            Console.WriteLine($"Disconnecting Channel {session.DiscordChannel.Name} from server {session.ArchipelagoSession.Socket.Uri}");
            if (session.ArchipelagoSession.Socket.Connected) { await session.ArchipelagoSession.Socket.DisconnectAsync(); }
            if (session.AuxiliarySessions.Count > 0)
            {
                foreach (var auxSession in session.AuxiliarySessions.Values)
                {
                    if (auxSession.Socket.Connected) { await auxSession.Socket.DisconnectAsync(); }
                }
                session.AuxiliarySessions.Clear();
            }
        }
        /// <summary>
        /// Creates the archipelago handlers for the given auxiliary slot connection
        /// </summary>
        /// <param name="botSession"></param>
        /// <param name="discordBot"></param>
        /// <param name="AuxiliaryConnection"></param>
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession, ArchipelagoSession AuxiliaryConnection)
        {
            AuxiliaryConnection.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;

            void MessageLog_OnMessageReceived(LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) return;
                switch (message)
                {
                    case HintItemSendLogMessage hintItemSendLogMessage:
                        var queuedMessage = new MessageQueueData.QueuedMessage(message.ColorLogMessage(), message.ToString(), message.GetUserPings(botSession));
                        botSession.QueueMessageForChannel(queuedMessage);
                        break;
                    case CommandResultLogMessage commandResultLogMessage:
                        botSession.QueueMessageForChannel(commandResultLogMessage.ToString());
                        break;
                }
            }
        }
        /// <summary>
        /// Creates the archipelago handlers for the main slot connection
        /// </summary>
        /// <param name="botSession"></param>
        /// <param name="discordBot"></param>
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession)
        {
            botSession.ArchipelagoSession.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            botSession.ArchipelagoSession.Socket.SocketClosed += async (reason) =>
            {
                if (!botSession.ParentBot.ActiveSessions.ContainsKey(botSession.DiscordChannel.Id)) { return; } //Bot was disconnected already
                await CleanAndCloseChannel(botSession.ParentBot, botSession.DiscordChannel.Id);
                botSession.QueueMessageForChannel($"Connection closed:\n{reason}");
            };
            void MessageLog_OnMessageReceived(LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) { return; }
                var queuedMessage = new MessageQueueData.QueuedMessage(message.ColorLogMessage(), message.ToString(), message.GetUserPings(botSession));
                botSession.QueueMessageForChannel(queuedMessage);
            }
        }
    }
}
