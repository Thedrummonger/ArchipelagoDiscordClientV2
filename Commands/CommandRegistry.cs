using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public class CommandRegistry(DiscordBot bot)
    {
        private Dictionary<string, SlashCommand> Commands = new Dictionary<string, SlashCommand>();

        public SlashCommand AddCommand(string Name)
        {
            Commands[Name] = new SlashCommand() { Name = Name };
            return Commands[Name];
        }
        public SlashCommand? GetCommand(string Name)
        {
            if (!Commands.TryGetValue(Name, out SlashCommand? slashCommand)) { return null; }
            return slashCommand;
        }
        public async Task Initialize()
        {
            var Client = bot.GetClient();
            foreach (var item in Commands)
            {
                var CommandProperties = item.Value.GetProperties() ?? throw new Exception($"Command {item.Key} has no properties");
                await Client.CreateGlobalApplicationCommandAsync(CommandProperties);
            }
        }
    }

    public class SlashCommand
    {
        public required string Name { get; set; }
        private SlashCommandProperties? Properties { get; set; }

        private Func<SocketSlashCommand, DiscordBot, Task> Execute = UnimplementedCommand;
        public SlashCommandProperties? GetProperties() => Properties;
        public Task ExecuteCommand(SocketSlashCommand c, DiscordBot d) => Execute(c, d);
        public SlashCommand SetProperties(SlashCommandBuilder _Properties)
        {
            _Properties.WithName(Name);
            Properties = _Properties.Build();
            return this;
        }
        public SlashCommand SetExecutionFunc(Func<SocketSlashCommand, DiscordBot, Task> func)
        {
            this.Execute = func;
            return this;
        }

        private static async Task UnimplementedCommand(SocketSlashCommand command, DiscordBot discordBot)
        {
            await command.RespondAsync("This command is not yet implemented.");
        }
    }

    public class CommandCreation
    {
        public static void CreateCommands(DiscordBot bot)
        {
            bot.commandRegistry.AddCommand("connect")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Connect this channel to an Archipelago server")
                    .AddOption("ip", ApplicationCommandOptionType.String, "Server IP", true)
                    .AddOption("port", ApplicationCommandOptionType.Integer, "Server Port", true)
                    .AddOption("game", ApplicationCommandOptionType.String, "Game name", true)
                    .AddOption("name", ApplicationCommandOptionType.String, "Player name", true)
                    .AddOption("password", ApplicationCommandOptionType.String, "Optional password", false)
                ).SetExecutionFunc(ConnectionCommands.HandleConnectCommand);

            bot.commandRegistry.AddCommand("disconnect")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Disconnect this channel from the Archipelago server")
                ).SetExecutionFunc(ConnectionCommands.HandleDisconnectCommand);

            bot.commandRegistry.AddCommand("show_hints")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Shows all hint for the current player")
                ).SetExecutionFunc(HintCommands.HandleShowHintsCommand);
        }
    }
}
