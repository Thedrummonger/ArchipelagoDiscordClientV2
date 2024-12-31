using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public class CommandRegistry
    {
        private DiscordBot _discordBot;
        public CommandRegistry(DiscordBot bot) 
        {
            _discordBot = bot;
            var CommandCreation = new CommandCreation();
            CommandCreation.CreateCommands(this);
        }
        public Dictionary<string, SlashCommand> Commands = new Dictionary<string, SlashCommand>();

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
            var Client = _discordBot.GetClient();
            foreach (var item in Commands)
            {
                Console.WriteLine($"Registering Command {item.Key}");
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
            if (!_Properties.Name.IsNullOrWhiteSpace()) 
            {
                throw new Exception($"Name is already set in the AddCommand funtition, do not add a name in the builder");
            }
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

    class CommandCreation
    {
        public void CreateCommands(CommandRegistry commandRegistry)
        {
            commandRegistry.AddCommand("connect")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Connect this channel to an Archipelago server")
                    .AddOption("ip", ApplicationCommandOptionType.String, "Server IP", true)
                    .AddOption("port", ApplicationCommandOptionType.Integer, "Server Port", true)
                    .AddOption("game", ApplicationCommandOptionType.String, "Game name", true)
                    .AddOption("name", ApplicationCommandOptionType.String, "Player name", true)
                    .AddOption("password", ApplicationCommandOptionType.String, "Optional password", false)
                ).SetExecutionFunc(ConnectionCommands.HandleConnectCommand);

            commandRegistry.AddCommand("disconnect")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Disconnect this channel from the Archipelago server")
                ).SetExecutionFunc(ConnectionCommands.HandleDisconnectCommand);

            commandRegistry.AddCommand("show_hints")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Shows all hint for the current player")
                    .AddOption("player", ApplicationCommandOptionType.String, "Player to get Hints for, defaults to connected player", false)
                ).SetExecutionFunc(HintCommands.HandleShowHintsCommand);

            commandRegistry.AddCommand("show_sessions")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Show all active Archipelago sessions in this server")
                ).SetExecutionFunc(ShowSessionCommands.HandleShowSessionsCommand);

            commandRegistry.AddCommand("show_channel_session")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Show the active Archipelago session for this channel")
                ).SetExecutionFunc(ShowSessionCommands.HandleShowChannelSessionCommand);

            commandRegistry.AddCommand("assign_user_to_player")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Assign discord user to archipelago player")
                    .AddOption("user", ApplicationCommandOptionType.User, "Discord user", true)
                    .AddOption("players", ApplicationCommandOptionType.String, "Comma-separated player names", true)
                ).SetExecutionFunc(HandleUserAssignmentCommands.HandleAssignUserToPlayer);

            commandRegistry.AddCommand("show_bot_settings")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Prints the current bot settings")
                ).SetExecutionFunc(OptionManagementCommands.HandleShowSettingsCommand);

            commandRegistry.AddCommand("edit_bot_settings")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Toggle the selected bot setting")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("setting")
                        .WithDescription("Select a setting to toggle")
                        .WithRequired(true)
                        .AddChoice("ignore_leave_join", (int)SettingEnum.IgnoreLeaveJoin)
                        .AddChoice("ignore_item_send", (int)SettingEnum.IgnoreItemSend)
                        .AddChoice("ignore_chats", (int)SettingEnum.IgnoreChats)
                        .AddChoice("ignore_connected_player_chats", (int)SettingEnum.IgnoreConnectedPlayerChats)
                        .WithType(ApplicationCommandOptionType.Integer)
                    )
                    .AddOption("value", ApplicationCommandOptionType.Boolean, "Value", false)
                ).SetExecutionFunc(OptionManagementCommands.HandleChangeSettingsCommand);

            commandRegistry.AddCommand("edit_ignored_tags")
                .SetProperties(new SlashCommandBuilder()
                    .WithDescription("Adds or removes ignored tags")
                    .AddOption("add", ApplicationCommandOptionType.Boolean, "true: add, false: remove", true)
                    .AddOption("tags", ApplicationCommandOptionType.String, "Comma-separated tags", true)
                ).SetExecutionFunc(OptionManagementCommands.HandleEditIngoredTagsCommand);
        }
    }
}
