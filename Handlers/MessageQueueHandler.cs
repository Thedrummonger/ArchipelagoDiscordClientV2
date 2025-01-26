using ArchipelagoDiscordClient;
using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueueData;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class ActiveSessionMessageQueue(DiscordBotData.DiscordBot discordBot, Sessions.ActiveBotSession ChannelSession)
    {
        private static readonly Tuple<string, string> Formatter = new("```ansi\n", "\n```");
        private static readonly string LineSeparator = "\n\n";
        public Queue<MessageQueueData.QueuedMessage> Queue = [];
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
                var messageBatch = new List<string>();
                var pingBatch = new HashSet<ulong>();
                try
                {
                    while (Queue.Count > 0)
                    {
                        var nextItem = Queue.Peek();
                        var simulatedMessage = GetFinalMessage([.. messageBatch, nextItem.Message]);
                        if (simulatedMessage.Length > Constants.DiscordRateLimits.DiscordEmbedMessageLimit)
                            break;

                        Queue.Dequeue();
                        messageBatch.Add(nextItem.Message);
                        foreach (var userId in nextItem.UsersToPing)
                            pingBatch.Add(userId);
                    }

                    var finalMessage = GetFinalMessage(messageBatch);
                    Console.WriteLine(finalMessage.Length);
                    QueuedAPIMessage queuedAPIMessage = new(CreatePingString(pingBatch), finalMessage);
                    discordBot.QueueAPIAction(ChannelSession.DiscordChannel, queuedAPIMessage);
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"Error Queueing message for channel: {ChannelSession.DiscordChannel.Id}\n{ChannelSession.ConnectionInfo.ToFormattedJson()}\n{ex}");
                }

                await Task.Delay(Constants.DiscordRateLimits.SendMessage);
            }
            Console.WriteLine($"Exited Message Queue for {ChannelSession.DiscordChannel.Id}");
            if (discordBot.ActiveSessions.ContainsKey(ChannelSession.DiscordChannel.Id))
                Console.WriteLine($"This was NOT intentional");

        }

        private static string GetFinalMessage(List<string> sendBatch) =>
            $"{Formatter.Item1}{string.Join(LineSeparator, sendBatch)}{Formatter.Item2}";

        private static string CreatePingString(HashSet<ulong> toPing) =>
            string.Join("", toPing.Select(user => $"<@{user}>"));
    }
    public class BotAPIRequestQueue(DiscordBotData.DiscordBot discordBot)
    {
        public bool IsProcessing = true;
        public Queue<(ISocketMessageChannel channel, IQueuedAPIAction Action)> Queue = [];
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
                        case QueuedAPIMessage messageAction:
                            _ = channel.SendMessageAsync(messageAction.Message, embed: messageAction.Embed);
                            break;
                    }

                }
                catch (Exception ex) { LogException(ex); }
                await Task.Delay(Constants.DiscordRateLimits.APICalls);
            }
            Console.WriteLine($"Exited Global API Processing Queue");
        }

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
