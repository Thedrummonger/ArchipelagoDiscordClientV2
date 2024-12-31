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
                        Console.WriteLine($"Queue had {queue.Count}");
                        var firstItem = queue.Peek();
                        if (firstItem.Channel is not SocketTextChannel channel) { throw new Exception("Channel from Queue was null, this should not happen!"); }
                        List<string> sendBatch = [];
                        int currentSize = Formatter.Item1.Length + Formatter.Item2.Length; // Start with the size of the formatter
                        while (queue.Count > 0)
                        {
                            //Account for the separator if this isn't the first message in the batch
                            int Padding = sendBatch.Count > 0 ? LineSeparator.Length : 0;
                            int nextMessageSize = queue.Peek().Message.Length + Padding;
                            //Break the loop if the message would go over the Discord message char limit
                            if (currentSize + nextMessageSize > DiscordMessageLimit) break; 
                            sendBatch.Add(queue.Dequeue().Message);
                            currentSize += nextMessageSize;
                        }

                        var formattedMessage = GetFinalMessage(sendBatch);
                        Console.WriteLine($"Sending Final message of size {formattedMessage.Length}");
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

        private static string GetFinalMessage(List<string> sendBatch)
        {
            return $"{Formatter.Item1}{string.Join(LineSeparator, sendBatch)}{Formatter.Item2}";
        }
    }
}
