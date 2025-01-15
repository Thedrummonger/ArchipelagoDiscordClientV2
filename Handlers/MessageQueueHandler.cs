using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class ActiveSessionMessageQueue(DiscordBotData.DiscordBot discordBot, Sessions.ActiveBotSession ChannelSession)
    {
        private static readonly Tuple<string, string> Formatter = new("```ansi\n", "\n```");
        private static readonly string LineSeparator = "\n\n";
        private static readonly int DiscordMessageLimit = 2000;
        private static readonly int DiscordChannelMessageRateLimit = 500; //Only 2 messages a second are allowed per channel
        public Queue<MessageQueueData.QueuedMessage> Queue = [];
        public async Task ProcessChannelMessages()
        {
            while (discordBot.ActiveSessions.ContainsKey(ChannelSession.DiscordChannel.Id))
            {
                if (Queue.Count == 0)
                {
                    await Task.Delay(100);
                    continue;
                }
                var messageBatch = new List<string>();
                var pingBatch = new HashSet<ulong>();

                while (Queue.Count > 0)
                {
                    var nextItem = Queue.Peek();
                    var simulatedMessage = GetFinalMessage([.. messageBatch, nextItem.Message], [.. pingBatch, .. nextItem.UsersToPing]);
                    if (simulatedMessage.Length > DiscordMessageLimit)
                        break;

                    Queue.Dequeue();
                    messageBatch.Add(nextItem.Message);
                    foreach (var userId in nextItem.UsersToPing)
                        pingBatch.Add(userId);
                }

                var finalMessage = GetFinalMessage(messageBatch, pingBatch);
                discordBot.QueueAPIAction(ChannelSession.DiscordChannel, finalMessage);

                await Task.Delay(DiscordChannelMessageRateLimit);
            }
        }

        private static string GetFinalMessage(List<string> sendBatch, HashSet<ulong> toPing) =>
            $"{Formatter.Item1}{string.Join(LineSeparator, sendBatch)}{Formatter.Item2}{CreatePingString(toPing)}";

        private static string CreatePingString(HashSet<ulong> toPing) =>
            string.Join("", toPing.Select(user => $"<@{user}>"));
    }
    public class BotAPIRequestQueue
    {
        public bool IsProcessing = true;
        private static readonly int DiscordAPIRequestRateLimit = 20; //Only 50 api calls per second are allowed globally for the bot
        public Queue<(ISocketMessageChannel channel, string message)> Queue = [];
        public async Task ProcessAPICalls()
        {
            while (IsProcessing)
            {
                if (Queue.Count > 0)
                {
                    var (channel, message) = Queue.Dequeue();
                    _ = channel.SendMessageAsync(message);
                }
                await Task.Delay(DiscordAPIRequestRateLimit);
            }
        }
    }
}
