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
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession)
        {
            botSession.ArchipelagoSession.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            botSession.ArchipelagoSession.Socket.SocketClosed += async (reason) =>
            {
                //To my knowledge this is never actually called in this code.
                //It seems to only trigger when the "Disconnect" command is run by the
                //bot and not when the AP server is closed, which is what this code is supposed to handle.
                //I'll leave it in for now, but detecting server closing is currently done in the `CheckServerConnection` function
                if (!botSession.ParentBot.ActiveSessions.ContainsKey(botSession.DiscordChannel.Id)) { return; } //Bot was disconnected already
                await CleanAndCloseChannel(botSession.ParentBot, botSession.DiscordChannel.Id);
                botSession.ParentBot.QueueAPIAction(botSession.DiscordChannel, $"Connection closed:\n{reason}");
            };
            void MessageLog_OnMessageReceived(LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) { return; }
                var queuedMessage = new MessageQueueData.QueuedMessage(message.ColorLogMessage(), message.ToString(), message.GetUserPings(botSession));
                botSession.QueueMessageForChannel(queuedMessage);
            }
        }

        public static async Task SessionDisconnectionHandler(this DiscordBot discordBot)
        {
            while (true)
            {
                ulong[] CurrentSessions = [.. discordBot.ActiveSessions.Keys];
                foreach (var i in CurrentSessions)
                {
                    if (!discordBot.ActiveSessions.TryGetValue(i, out var session)) continue;
                    if (!session.ArchipelagoSession.Socket.Connected)
                    {
                        await discordBot.CleanAndCloseChannel(i);
                        var server = $"{session.ConnectionInfo.IP}:{session.ConnectionInfo.Port}";
                        discordBot.QueueAPIAction(session.DiscordChannel, $"Disconnected from ${server}, Archipelago server closed");
                    }
                }
                await Task.Delay(500);
            }
        }

        public static async Task DisconnectAllClients(this DiscordBot botClient)
        {
            Console.WriteLine("Disconnecting all clients...");
            foreach (var session in botClient.ActiveSessions.Values)
            {
                Console.WriteLine(session.DiscordChannel.Name);
                await archipelagoConnectionHelpers.CleanAndCloseChannel(botClient, session.DiscordChannel.Id);
                botClient.QueueAPIAction(session.DiscordChannel, $"Connection closed, Bot has exited.");
            }
            Console.WriteLine("Waiting for Queue to clear...");
            while (botClient.DiscordAPIQueue.Queue.Count > 0)
            {
                await Task.Delay(20);
            }
            botClient.DiscordAPIQueue.IsProcessing = false;
            await Task.Delay(2000);
        }
    }
}
