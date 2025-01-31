﻿using Discord;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Data
{
    /// <summary>
    /// Contains data structures and utility methods for queuing and processing messages in the Discord bot.
    /// </summary>
    public static class MessageQueueData
    {
        /// <summary>
        /// Represents an action that can be enqueued for API processing.
        /// </summary>
        public interface IQueuedAPIAction { }
        /// <summary>
        /// Represents a queued message, containing optional embeds and a message string.
        /// </summary>
        public class QueuedMessage(IEnumerable<Embed> embeds, string? message = null) : IQueuedMessage, IQueuedAPIAction
        {
            public QueuedMessage(string message) : this([], message) { }
            public QueuedMessage(Embed? embed, string? message = null) : this(embed is null ? [] : [embed], message) { }

            public string? Message = message;
            public Embed[] Embeds = [.. embeds];
        }
        /// <summary>
        /// Enqueues an API action to be processed by the bot's API request queue.
        /// </summary>
        /// <param name="discordBot">The Discord bot instance managing the queue.</param>
        /// <param name="channel">The Discord channel where the action will be performed.</param>
        /// <param name="action">The API action to enqueue.</param>
        public static void QueueAPIAction(this DiscordBot discordBot, ISocketMessageChannel channel, IQueuedAPIAction action) =>
            discordBot.DiscordAPIQueue.Queue.Enqueue(new(channel, action));

        /// <summary>
        /// Represents a generic queued message.
        /// </summary>
        public interface IQueuedMessage { }
        /// <summary>
        /// Represents a queued item log message, containing message content and user pings.
        /// </summary>
        public class QueuedItemLogMessage(string message, string? raw = null, IEnumerable<ulong>? ToPing = null) : IQueuedMessage
        {
            public string Message = message;
            public string RawMessage = raw ?? message;
            public HashSet<ulong> UsersToPing = ToPing is null ? [] : [.. ToPing];
        }
        /// <summary>
        /// Enqueues a message to be sent to the Discord channel associated with the session.
        /// </summary>
        /// <param name="session">The active bot session.</param>
        /// <param name="message">The message to enqueue.</param>
        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, IQueuedMessage message) =>
            session.MessageQueue.Queue.Enqueue(message);
        /// <summary>
        /// Enqueues a plain text message to be sent to the Discord channel associated with the session.
        /// </summary>
        /// <param name="session">The active bot session.</param>
        /// <param name="message">The message text to enqueue.</param>
        public static void QueueMessageForChannel(this Sessions.ActiveBotSession session, string message) =>
            session.QueueMessageForChannel(new QueuedMessage(message));

    }
}
