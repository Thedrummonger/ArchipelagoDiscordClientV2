using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class MessageQueueData
    {
        public interface IQueuedMessage { }
        public class QueuedItemLogMessage(string message, string? raw = null, IEnumerable<ulong>? ToPing = null) : IQueuedMessage
        {
            public string Message = message;
            public string RawMessage = raw ?? message;
            public HashSet<ulong> UsersToPing = ToPing is null ? [] : [.. ToPing];
        }
        public class QueuedLogMessage : IQueuedMessage
        {
            public QueuedLogMessage(string? message = null, string? EmbedMessage = null)
            {
                Message = message;
                Embed = EmbedMessage is null ? null : new EmbedBuilder().WithDescription(EmbedMessage).Build();
            }
            public QueuedLogMessage(EmbedBuilder EmbedMessage, string? message = null)
            {
                Message = message;
                Embed = EmbedMessage.Build();
            }
            public QueuedLogMessage(Embed EmbedMessage, string? message = null)
            {
                Message = message;
                Embed = EmbedMessage;
            }
            public string? Message;
            public Embed? Embed;
        }

        public interface IQueuedAPIAction { }
        public class QueuedAPIMessage : IQueuedAPIAction
        {
            public QueuedAPIMessage(string? message = null, string? EmbedMessage = null)
            {
                Message = message;
                Embed = EmbedMessage is null ? null : new EmbedBuilder().WithDescription(EmbedMessage).Build();
            }
            public QueuedAPIMessage(EmbedBuilder? EmbedMessage, string? message = null)
            {
                Message = message;
                Embed = EmbedMessage?.Build() ?? null;
            }
            public QueuedAPIMessage(Embed? EmbedMessage, string? message = null)
            {
                Message = message;
                Embed = EmbedMessage;
            }
            public string? Message;
            public Embed? Embed;
        }

        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, IQueuedMessage Message) =>
            session.MessageQueue.Queue.Enqueue(Message);
        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, string Message) =>
            session.QueueMessageForChannel(new QueuedLogMessage(Message));
        public static void QueueAPIAction(this DiscordBot discordBot, ISocketMessageChannel channel, IQueuedAPIAction action) =>
            discordBot.DiscordAPIQueue.Queue.Enqueue(new(channel, action));
    }
}
