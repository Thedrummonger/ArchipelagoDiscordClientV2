using ArchipelagoDiscordClientLegacy.Data;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class MessageQueueHandler(DiscordBotData.DiscordBot discordBot)
    {
        public Dictionary<ulong, Queue<MessageQueue.QueuedMessage>> MessageQueue = [];
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
                        SocketTextChannel? channel = queue.Peek().Channel;
                        List<string> sendBatch = new();
                        while (queue.Count > 0 && GetFinalMessage(sendBatch).Length < 2000)
                        {
                            sendBatch.Add(queue.Dequeue().Message);
                        }
                        var formattedMessage = GetFinalMessage(sendBatch);
                        if (channel is null) { throw new Exception("Channel from Queue was null, this should not happen!"); }
                        _ = channel.SendMessageAsync(formattedMessage);
                        await Task.Delay(discordBot.appSettings.DiscordRateLimitDelay);
                    }
                }
                //Wait before processing more messages to avoid rate limit
                //This await could probably be removed? or at least significantly lowered. 
                await Task.Delay(discordBot.appSettings.DiscordRateLimitDelay);
            }
        }

        private static string GetFinalMessage(List<string> sendBatch)
        {
            return $"```ansi\n{string.Join("\n\n", sendBatch)}\n```";
        }
    }
}
