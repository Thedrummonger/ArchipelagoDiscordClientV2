using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using ArchipelagoDiscordClientLegacy.Commands;
using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using TDMUtils;
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
        /// <summary>
        /// Creates the archipelago handlers for the given auxiliary slot connection
        /// </summary>
        /// <param name="botSession"></param>
        /// <param name="discordBot"></param>
        /// <param name="AuxiliaryConnection"></param>
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession, DiscordBot discordBot, ArchipelagoSession AuxiliaryConnection)
        {
            AuxiliaryConnection.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;

            void MessageLog_OnMessageReceived(LogMessage message)
            {
                if (string.IsNullOrWhiteSpace(message.ToString())) { return; }
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) return;

                switch (message)
                {
                    case HintItemSendLogMessage hintItemSendLogMessage:
                        var MessageString = message.ColorLogMessage();
                        var queuedMessage = botSession.DiscordChannel.CreateSimpleQueuedMessage(MessageString, message.ToString());
                        queuedMessage.UsersToPing = message.GetUserPings(botSession);
                        discordBot.QueueMessage(queuedMessage);
                        break;
                    case CommandResultLogMessage commandResultLogMessage:
                        discordBot.QueueMessage(botSession.DiscordChannel, commandResultLogMessage.ToString());
                        break;
                }
            }
        }
        /// <summary>
        /// Creates the archipelago handlers for the main slot connection
        /// </summary>
        /// <param name="botSession"></param>
        /// <param name="discordBot"></param>
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession, DiscordBot discordBot)
        {
            botSession.archipelagoSession.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            botSession.archipelagoSession.Socket.SocketClosed += async (reason) =>
            {
                if (!discordBot.ActiveSessions.ContainsKey(botSession.DiscordChannel.Id)) { return; } //Bot was disconnected already
                await CleanAndCloseChannel(discordBot, botSession.DiscordChannel.Id);
                discordBot.QueueMessage(botSession.DiscordChannel!, $"Connection closed:\n{reason}");
            };
            void MessageLog_OnMessageReceived(LogMessage message)
            {
                Console.WriteLine($"{botSession.archipelagoSession.Players.ActivePlayer.Name}|{message.GetType()}");
                if (string.IsNullOrWhiteSpace(message.ToString())) { return; }
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) { return; }

                var MessageString = message.ColorLogMessage();
                var queuedMessage = botSession.DiscordChannel.CreateSimpleQueuedMessage(MessageString, message.ToString());
                queuedMessage.UsersToPing = message.GetUserPings(botSession);
                discordBot.QueueMessage(queuedMessage);
            }
        }
    }
}
