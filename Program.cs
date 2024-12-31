using Discord;
using Discord.WebSocket;
using Archipelago.MultiClient.Net;
using Discord.Net;
using System.Text;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.Enums;
using System.Text.RegularExpressions;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using ArchipelagoDiscordClientLegacy.Handlers;
using ArchipelagoDiscordClientLegacy.Data;

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
            var Config = TDMUtils.DataFileUtilities.LoadObjectFromFileOrDefault(Constants.Paths.ConfigFile, new AppSettings(), true);
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