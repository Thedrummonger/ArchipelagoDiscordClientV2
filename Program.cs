using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using ArchipelagoDiscordClientLegacy.Data;
using TDMUtils;
using ArchipelagoDiscordClientLegacy.Helpers;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;
using System.Diagnostics;

namespace ArchipelagoDiscordClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var program = new Program();
            await program.RunBotAsync();
        }

        public async Task RunBotAsync()
        {
            if (!Path.Exists(Constants.Paths.BaseFilePath)) { Directory.CreateDirectory(Constants.Paths.BaseFilePath); }
            var Config = DataFileUtilities.LoadObjectFromFileOrDefault(Constants.Paths.ConfigFile, new AppSettings(), true);
            if (Config.BotToken.IsNullOrWhiteSpace()) 
            {
                Console.WriteLine("Please enter your bot key:");
                var key = Console.ReadLine()??"";
                Config.BotToken = key;
                File.WriteAllText(Constants.Paths.ConfigFile, Config.ToFormattedJson());
            }
            if (Config.BotToken.IsNullOrWhiteSpace()) { throw new Exception($"Bot key not valid"); }
            DiscordBot BotClient = new DiscordBot(Config);

            BotClient.ConnectionCache = DataFileUtilities.LoadObjectFromFileOrDefault(Constants.Paths.ConnectionCache, new Dictionary<ulong, SessionContructor>(), true);

            BotClient.GetClient().Ready += BotClient.commandRegistry.Initialize;
            BotClient.GetClient().SlashCommandExecuted += BotClient.CommandHandler.HandleSlashCommand;
            BotClient.GetClient().MessageReceived += BotClient.DiscordMessageHandler.HandleDiscordMessageReceivedAsync;
            BotClient.GetClient().Log += (logMessage) => {
                Console.WriteLine(logMessage.ToString());
                return Task.CompletedTask;
            };

            await BotClient.Start();

            //Run a background task to constantly send messages in the send queue
            _ = Task.Run(BotClient.MessageQueueHandler.ProcessMessageQueueAsync);

            _ = Task.Run(() => CheckServerLife(BotClient));

            //TODO, maybe just replace this with a loop to handle console commands on the server it's self
            await Task.Delay(-1);
        }

        private async Task CheckServerLife(DiscordBot bot)
        {
            //Archipelagos SocketClosed event doesn't seem to trigger when the server is closed?
            //for now we'll just check the connections manually every few seconds.
            while (true) 
            { 
                List<ulong> SessionKeys = [.. bot.ActiveSessions.Keys];
                Debug.WriteLine($"Checking Sessions {SessionKeys.ToFormattedJson()}");
                foreach(var i in SessionKeys)
                {
                    if (!bot.ActiveSessions.TryGetValue(i, out ActiveBotSession? ActiveSession)) { continue; }
                    try { ActiveSession.archipelagoSession.DataStorage.GetClientStatus(); }
                    catch
                    {
                        if (!bot.ActiveSessions.ContainsKey(i)) { continue; }   
                        _ = archipelagoConnectionHelpers.CleanAndCloseChannel(bot, i);
                        bot.QueueMessage(ActiveSession.DiscordChannel!, $"Connection closed:\nServer no longer reachable");
                    }
                }
                await Task.Delay(5000);
            }
        }
    }
}