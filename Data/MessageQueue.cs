using Discord.WebSocket;
using TDMUtils;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class MessageQueueData
    {
        public class QueuedMessage
        {
            public required SocketTextChannel? Channel = null;
            public required string Message = "";
            public string RawMessage = "";
            public HashSet<ulong> UsersToPing = [];
        }
        public static QueuedMessage CreateSimpleQueuedMessage(this SocketTextChannel channel, string Message)
        {
            return new QueuedMessage
            {
                Message = Message,
                RawMessage = Message,
                Channel = channel,
                UsersToPing = []
            };
        }

        public static void QueueMessage(this DiscordBotData.DiscordBot discordBot, QueuedMessage Message)
        {
            discordBot.MessageQueueHandler.MessageQueue.SetIfEmpty(Message.Channel!.Id, new Queue<QueuedMessage>());
            discordBot.MessageQueueHandler.MessageQueue[Message.Channel!.Id].Enqueue(Message);
        }
        public static void QueueMessage(this QueuedMessage Message, DiscordBotData.DiscordBot discordBot) => discordBot.QueueMessage(Message);

        public static void QueueMessage(this DiscordBotData.DiscordBot discordBot, SocketTextChannel channel, string Message) =>
            discordBot.QueueMessage(channel.CreateSimpleQueuedMessage(Message));

        public static void QueueMessage(this SocketTextChannel channel, DiscordBotData.DiscordBot discordBot, string Message) =>
            discordBot.QueueMessage(channel.CreateSimpleQueuedMessage(Message));
    }
}
