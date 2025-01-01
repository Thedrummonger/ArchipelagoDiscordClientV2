using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class MessageQueueHandler(DiscordBotData.DiscordBot discordBot)
    {
        public Dictionary<ulong, Queue<MessageQueueData.QueuedMessage>> MessageQueue = [];
        public async Task ProcessMessageQueueAsync()
        {
            //Discord only allows 50 api calls per second, or 1 every 20 milliseconds
            //With how fast archipelago sends data sometimes, it's really easy to hit this
            //To get around this, anytime I send a message to a discord channel I send it through this queue instead.
            //The current delay of 500 ms per action is way overkill and can probably be brought down eventually.
            while (true)
            {
                foreach (var queue in MessageQueue.Values)
                {
                    if (queue.Count > 0)
                    {
                        var FirstItem = queue.Peek();
                        if (FirstItem.Channel is not SocketTextChannel channel)
                            throw new Exception("Channel from Queue was null, this should not happen!");

                        List<string> sendBatch = [];
                        HashSet<SocketUser> ToPing = [];

                        while (queue.Count > 0)
                        {
                            var PeekedItem = queue.Peek();
                            List<string> SimulatedMessageBatch = [.. sendBatch, .. new string[] { PeekedItem.Message }];
                            HashSet<SocketUser> SimulatedToPing = [.. ToPing, .. PeekedItem.UsersToPing];
                            string SimulatedFinalMessage = GetFinalMessage(SimulatedMessageBatch, SimulatedToPing);
                            if (SimulatedFinalMessage.Length > DiscordMessageLimit) break;

                            var QueuedItem = queue.Dequeue();
                            sendBatch.Add(QueuedItem.Message);
                            foreach(var user in PeekedItem.UsersToPing)
                                ToPing.Add(user);
                        }

                        var formattedMessage = GetFinalMessage(sendBatch, ToPing);
                        _ = channel.SendMessageAsync(formattedMessage);
                        await Task.Delay(discordBot.appSettings.DiscordRateLimitDelay);
                    }
                }
                //Wait before processing more messages to avoid rate limit
                //TODO: This await could probably be removed?
                await Task.Delay(discordBot.appSettings.DiscordRateLimitDelay);
            }
        }

        private static readonly Tuple<string, string> Formatter = new("```ansi\n", "\n```");
        private static readonly string LineSeparator = "\n\n";
        private static readonly int DiscordMessageLimit = 2000;

        private static string GetFinalMessage(List<string> sendBatch, HashSet<SocketUser> ToPing)
        {
            return $"{Formatter.Item1}{string.Join(LineSeparator, sendBatch)}{Formatter.Item2}{CreatePingString(ToPing)}";
        }
        private static string CreatePing(SocketUser user) => $"<@{user.Id}>";
        private static string CreatePingString(HashSet<SocketUser> ToPing) => string.Join("", ToPing.Select(CreatePing));
    }
}
