using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDMUtils;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class MessageQueue
    {
        public class QueuedMessage
        {
            public SocketTextChannel? Channel = null;
            public string Message = "";
            public string RawMessage = "";
            public SocketUser? UserToPing = null;
        }
        public static QueuedMessage CreateSimpleQueuedMessage(string Message, SocketTextChannel channel)
        {
            return new QueuedMessage
            {
                Message = Message,
                RawMessage = Message,
                Channel = channel,
                UserToPing = null
            };
        }
        public static QueuedMessage CreateSimpleQueuedMessage(this SocketTextChannel channel, string Message)
        {
            return CreateSimpleQueuedMessage(Message, channel);
        }

        public static void QueueMessage(this DiscordBotData.DiscordBot discordBot, QueuedMessage Message)
        {
            discordBot.MessageQueueHandler.MessageQueue.SetIfEmpty(Message.Channel!.Id, new Queue<QueuedMessage>());
            discordBot.MessageQueueHandler.MessageQueue[Message.Channel!.Id].Enqueue(Message);
        }
        public static void QueueMessage(this QueuedMessage Message, DiscordBotData.DiscordBot discordBot) => discordBot.QueueMessage(Message);

        public static void QueueSimpleMessage(this DiscordBotData.DiscordBot discordBot, SocketTextChannel channel, string Message) =>
            discordBot.QueueMessage(CreateSimpleQueuedMessage(Message, channel));
    }
}
