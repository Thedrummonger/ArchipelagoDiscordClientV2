using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Net.WebSockets;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueueData;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    /// <summary>
    /// Manages a queue of messages for an active Archipelago session and ensures they are processed within Discord rate limits.
    /// </summary>
    public class ActiveSessionMessageQueue(DiscordBotData.DiscordBot discordBot, Sessions.ActiveBotSession ChannelSession)
    {
        private static readonly Tuple<string, string> Formatter = new("```ansi\n", "\n```");
        private static readonly string LineSeparator = "\n\n";
        public Queue<IQueuedMessage> Queue = [];
        /// <summary>
        /// Continuously processes and sends queued messages for the session while the session remains active.
        /// </summary>
        public async Task ProcessChannelMessages()
        {
            while (discordBot.ActiveSessions.ContainsKey(ChannelSession.DiscordChannel.Id))
            {
                if (Program.ShowHeartbeat) { Console.WriteLine($"Message Queue Heartbeat: {ChannelSession.DiscordChannel.Id}"); }
                if (Queue.Count == 0)
                {
                    await Task.Delay(Constants.DiscordRateLimits.IdleDelay);
                    continue;
                }

                List<Embed> items = [];
                List<string> Set1;
                List<string> Set2;
                switch (Queue.Peek())
                {
                    case QueuedTypedMessage:
                        Set1 = GetSimpleMessagesForEmbed(Constants.DiscordRateLimits.DiscordEmbedMessageLimit);
                        Set2 = GetSimpleMessagesForEmbed(Constants.DiscordRateLimits.DiscordEmbedTotalLimit - GetFinalMessage(Set1).Length);
                        items.Add(new EmbedBuilder().WithDescription(string.Join('\n', Set1)).Build());
                        if (Set2.Count > 0)
                            items.Add(new EmbedBuilder().WithDescription(string.Join('\n', Set2)).Build());
                        discordBot.QueueAPIAction(ChannelSession.DiscordChannel, new QueuedMessage(items));
                        break;

                    case QueuedItemLogMessage:
                        Set1 = GetItemLogMessagesForEmbed(Constants.DiscordRateLimits.DiscordEmbedMessageLimit, out var PingSet1);
                        Set2 = GetItemLogMessagesForEmbed(Constants.DiscordRateLimits.DiscordEmbedTotalLimit - GetFinalMessage(Set1).Length, out var PingSet2);
                        items.Add(new EmbedBuilder().WithDescription(GetFinalMessage(Set1)).Build());
                        if (Set2.Count > 0)
                            items.Add(new EmbedBuilder().WithDescription(GetFinalMessage(Set2)).Build());
                        QueuedMessage queuedAPIMessage = new(items, CreatePingString([.. PingSet1, .. PingSet2]));
                        discordBot.QueueAPIAction(ChannelSession.DiscordChannel, queuedAPIMessage);
                        break;

                    case QueuedMessage queuedMessage:
                        Queue.Dequeue();
                        discordBot.QueueAPIAction(ChannelSession.DiscordChannel, queuedMessage);
                        break;
                }

                await Task.Delay(Constants.DiscordRateLimits.SendMessage);
            }
            Console.WriteLine($"Exited Message Queue for {ChannelSession.DiscordChannel.Id}");
            if (discordBot.ActiveSessions.ContainsKey(ChannelSession.DiscordChannel.Id))
                Console.WriteLine($"This was NOT intentional");

        }

        /// <summary>
        /// Retrieves messages for an embed, ensuring they do not exceed the Discord character limit.
        /// </summary>
        /// <param name="CharLimit">The maximum number of characters allowed in the message.</param>
        /// <param name="UserPings">A set of user IDs to ping in the message.</param>
        /// <returns>A list of messages that fit within the character limit.</returns>
        List<string> GetItemLogMessagesForEmbed(int CharLimit, out HashSet<ulong> UserPings)
        {
            UserPings = [];
            if (Queue.Count == 0) { return []; }
            var messageBatch = new List<string>();
            while (Queue.Count > 0)
            {
                var nextItem = Queue.Peek();
                if (nextItem is not QueuedItemLogMessage NextItemLogMessage) break;
                var simulatedMessage = GetFinalMessage([.. messageBatch, NextItemLogMessage.Message]);
                if (simulatedMessage.Length > CharLimit)
                    break;

                Queue.Dequeue();
                messageBatch.Add(NextItemLogMessage.Message!);
                foreach (var userId in NextItemLogMessage.UsersToPing)
                    UserPings.Add(userId);
            }
            return messageBatch;
        }

        List<string> GetSimpleMessagesForEmbed(int CharLimit)
        {
            if (Queue.Count == 0) { return []; }
            var messageBatch = new List<string>();
            Type? TargetType = null;
            while (Queue.Count > 0)
            {
                var nextItem = Queue.Peek();
                if (nextItem is not QueuedTypedMessage NextItemLogMessage) break;
                TargetType ??= NextItemLogMessage.Type;
                if (NextItemLogMessage.Type != TargetType) break;
                var simulatedMessage = string.Join('\n', [.. messageBatch, NextItemLogMessage.Message]);
                if (simulatedMessage.Length > CharLimit)
                    break;

                Queue.Dequeue();
                messageBatch.Add(NextItemLogMessage.Message!);
            }
            return messageBatch;
        }
        /// <summary>
        /// Formats a batch of messages with ANSI code block formatting.
        /// </summary>
        private static string GetFinalMessage(List<string> sendBatch) =>
            $"{Formatter.Item1}{string.Join(LineSeparator, sendBatch)}{Formatter.Item2}";
        /// <summary>
        /// Generates a mention string for all users to be pinged in a message.
        /// </summary>
        private static string CreatePingString(HashSet<ulong> toPing) =>
            string.Join("", toPing.Select(user => $"<@{user}>"));
    }
    /// <summary>
    /// Manages a queue for API requests sent by the bot to Discord.
    /// </summary>
    public class BotAPIRequestQueue(DiscordBotData.DiscordBot discordBot)
    {
        /// <summary>
        /// Indicates whether the queue should continue processing API requests.
        /// </summary>
        public bool IsProcessing = true;
        public Queue<(ISocketMessageChannel channel, IQueuedAPIAction Action)> Queue = [];
        /// <summary>
        /// Continuously processes and sends queued API requests while the bot is running.
        /// </summary>
        public async Task ProcessAPICalls()
        {
            while (IsProcessing)
            {
                if (Program.ShowHeartbeat) { Console.WriteLine($"Master API Queue Heartbeat"); }
                if (Queue.Count == 0 || discordBot.Client.ConnectionState != ConnectionState.Connected)
                {
                    await Task.Delay(Constants.DiscordRateLimits.IdleDelay);
                    continue;
                }
                try
                {
                    var (channel, action) = Queue.Dequeue();
                    if (channel is null || action is null) continue;

                    switch (action)
                    {
                        case QueuedMessage messageAction:
                            _ = channel.SendMessageAsync(messageAction.Message, embeds: messageAction.Embeds);
                            break;
                    }

                }
                catch (Exception ex) { LogException(ex); }
                await Task.Delay(Constants.DiscordRateLimits.APICalls);
            }
            Console.WriteLine($"Exited Global API Processing Queue");
        }
        /// <summary>
        /// Logs exceptions that occur while processing API requests.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        private static void LogException(Exception ex)
        {
            switch (ex)
            {
                case HttpException httpEx:
                    Console.WriteLine($"Http Exception while processing API request\n{httpEx}");
                    break;
                case RateLimitedException rateLimitedEx:
                    Console.WriteLine($"Rate Limited Exception while processing API request\n{rateLimitedEx}");
                    break;
                case WebSocketException wsEx:
                    Console.WriteLine($"WebSocket Exception while processing API request\n{wsEx}");
                    break;
                case TaskCanceledException tcEx:
                    Console.WriteLine($"Task Canceled while processing API request\n{tcEx}");
                    break;
                default:
                    Console.WriteLine($"error while processing API request\n{ex}");
                    break;
            }
        }
    }
}
