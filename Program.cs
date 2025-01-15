using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

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
                var key = Console.ReadLine() ?? "";
                Config.BotToken = key;
                File.WriteAllText(Constants.Paths.ConfigFile, Config.ToFormattedJson());
            }
            if (Config.BotToken.IsNullOrWhiteSpace()) { throw new Exception($"Bot key not valid"); }
            DiscordBot BotClient = new DiscordBot(Config);

            BotClient.ConnectionCache = DataFileUtilities.LoadObjectFromFileOrDefault(Constants.Paths.ConnectionCache, new Dictionary<ulong, SessionConstructor>(), true);

            BotClient.GetClient().Ready += BotClient.commandRegistry.Initialize;
            BotClient.GetClient().SlashCommandExecuted += BotClient.CommandHandler.HandleSlashCommand;
            BotClient.GetClient().MessageReceived += BotClient.DiscordMessageHandler.HandleDiscordMessageReceivedAsync;
            BotClient.GetClient().Log += (logMessage) =>
            {
                Console.WriteLine(logMessage.ToString());
                return Task.CompletedTask;
            };

            await BotClient.Start();

            //Run a background task to constantly process API requests
            _ = Task.Run(BotClient.DiscordAPIQueue.ProcessAPICalls);

            RunUserInputLoop();

            DisconnectAllClients(BotClient);
        }

        private async void DisconnectAllClients(DiscordBot botClient)
        {
            Console.WriteLine("Disconnecting all clients...");
            foreach (var session in botClient.ActiveSessions.Values)
            {
                Console.WriteLine(session.DiscordChannel.Name);
                await archipelagoConnectionHelpers.CleanAndCloseChannel(botClient, session.DiscordChannel.Id);
                botClient.QueueAPIAction(session.DiscordChannel, $"Connection closed, Bot has exited.");
            }
            Console.WriteLine("Waiting for Queue to clear...");
            while (botClient.DiscordAPIQueue.Queue.Count > 0)
            {
                await Task.Delay(20);
            }
            botClient.DiscordAPIQueue.IsProcessing = false;
        }

        private void RunUserInputLoop()
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (input is null) continue;
                if (input == "exit") break;
                ProcessConsoleCommand(input);
            }
        }

        private void ProcessConsoleCommand(string Input)
        {
            if (false) //Eventually replace false with a check for a valid command
            {
                //Process the command
            }
            else
            {
                Console.WriteLine("Invalid Command");
            }
        }
    }
}