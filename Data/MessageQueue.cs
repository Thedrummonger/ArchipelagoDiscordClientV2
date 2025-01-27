using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class MessageQueueData
    {
        public interface IQueuedAPIAction { }
        public class QueuedMessage(IEnumerable<Embed> embeds, string? message = null) : IQueuedMessage, IQueuedAPIAction
        {
            public QueuedMessage(string message) : this([], message) { }
            public QueuedMessage(Embed? embed, string? message = null) : this(embed is null ? [] : [embed], message) { }

            public string? Message = message;
            public Embed[] Embeds = [.. embeds];
        }
        public static void QueueAPIAction(this DiscordBot discordBot, ISocketMessageChannel channel, IQueuedAPIAction action) =>
            discordBot.DiscordAPIQueue.Queue.Enqueue(new(channel, action));


        public interface IQueuedMessage { }
        public class QueuedItemLogMessage(string message, string? raw = null, IEnumerable<ulong>? ToPing = null) : IQueuedMessage
        {
            public string Message = message;
            public string RawMessage = raw ?? message;
            public HashSet<ulong> UsersToPing = ToPing is null ? [] : [.. ToPing];
        }

        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, IQueuedMessage Message) =>
            session.MessageQueue.Queue.Enqueue(Message);
        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, string Message) =>
            session.QueueMessageForChannel(new QueuedMessage(Message));

    }
}
