using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class MessageQueueData
    {
        public class QueuedMessage(string message, string? raw = null, IEnumerable<ulong>? ToPing = null)
        {
            public string Message = message;
            public string RawMessage = raw ?? message;
            public HashSet<ulong> UsersToPing = ToPing is null ? [] : [.. ToPing];
        }

        public interface IQueuedAPIAction { }
        public class QueuedAPIMessage : IQueuedAPIAction
        {
            public QueuedAPIMessage(string? message = null, string? EmbedMessage = null)
            {
                Message = message;
                Embed = EmbedMessage is null ? null : new EmbedBuilder().WithDescription(EmbedMessage).Build();
            }
            public QueuedAPIMessage(EmbedBuilder EmbedMessage, string? message = null)
            {
                Message = message;
                Embed = EmbedMessage.Build();
            }
            public QueuedAPIMessage(Embed EmbedMessage, string? message = null)
            {
                Message = message;
                Embed = EmbedMessage;
            }
            public string? Message;
            public Embed? Embed;
        }

        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, QueuedMessage Message) =>
            session.MessageQueue.Queue.Enqueue(Message);
        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, string Message) =>
            session.QueueMessageForChannel(new QueuedMessage(Message));
        public static void QueueAPIAction(this DiscordBot discordBot, ISocketMessageChannel channel, IQueuedAPIAction action) =>
            discordBot.DiscordAPIQueue.Queue.Enqueue(new(channel, action));
    }
}
