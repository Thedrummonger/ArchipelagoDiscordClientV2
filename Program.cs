using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using ArchipelagoDiscordClientLegacy.Data;
using TDMUtils;

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

            //TODO, maybe just replace this with a loop to handle console commands on the server it's self
            await Task.Delay(-1);
        }

    }
}