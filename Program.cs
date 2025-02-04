using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Handlers;
using ArchipelagoDiscordClientLegacy.Helpers;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy
{
    public class Program
    {
        public static bool ShowHeartbeat = false;
        static async Task Main(string[] args) => await RunBotAsync();

        public static async Task RunBotAsync()
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
            DiscordBot BotClient = new(Config)
            {
                ConnectionCache = DataFileUtilities.LoadObjectFromFileOrDefault(Constants.Paths.ConnectionCache, new Dictionary<ulong, SessionConstructor>(), true)
            };

            BotClient.Client.Ready += BotClient.commandRegistry.RegisterSlashCommands;
            BotClient.Client.SlashCommandExecuted += BotClient.CommandHandler.HandleSlashCommand;
            BotClient.Client.MessageReceived += BotClient.DiscordMessageHandler.HandleDiscordMessageReceivedAsync;
            BotClient.Client.Log += (logMessage) =>
            {
                Console.WriteLine(logMessage.ToString());
                return Task.CompletedTask;
            };

            await BotClient.Start();

            //Run a background task to constantly process API requests
            _ = Task.Run(BotClient.DiscordAPIQueue.ProcessAPICalls);

            //Run a background loop to monitor server connections and clean up any closed or abandoned server connections
            _ = Task.Run(BotClient.MonitorAndHandleAPServerClose);


            //Allow for console commands, this loop will not exit unless the exit command is entered
            ConsoleCommandHandlers.RegisterCommands();
            ConsoleCommandHandlers.RunUserInputLoop(BotClient);

            //If the above loop does exit, most likely because the exit command has been executed, gracefully close all connections before closing the application
            await BotClient.DisconnectAllClients();
        }
    }
}