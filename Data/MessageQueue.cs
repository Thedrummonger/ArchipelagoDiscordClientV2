using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class MessageQueueData
    {
        public class QueuedMessage
        {
            public QueuedMessage(string message, string? raw = null, IEnumerable<ulong>? ToPing = null)
            {
                UsersToPing = ToPing is null ? [] : [.. ToPing];
                RawMessage = raw ?? Message;
                Message = message;
            }
            public string Message = "";
            public string RawMessage = "";
            public HashSet<ulong> UsersToPing;
        }

        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, QueuedMessage Message)
        {
            session.MessageQueue.Queue.Enqueue(Message);
        }
        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, string Message)
        {
            session.QueueMessageForChannel(new QueuedMessage(Message));
        }
        public static void QueueAPIAction(this DiscordBot discordBot, ISocketMessageChannel channel, string Message)
        {
            discordBot.DiscordAPIQueue.Queue.Enqueue(new(channel, Message));
        }
    }
}
