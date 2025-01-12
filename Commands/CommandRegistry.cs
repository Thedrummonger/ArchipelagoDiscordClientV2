using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
using System.Diagnostics;
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
        public Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>();

        public void AddCommand(ICommand command)
        {
            Commands[command.Name] = command;
        }
        public ICommand? GetCommand(string Name)
        {
            if (!Commands.TryGetValue(Name, out ICommand? value)) { return null; }
            return value;
        }
        public async Task Initialize()
        {
            await _discordBot.GetClient().BulkOverwriteGlobalApplicationCommandsAsync(Commands.Values.Select(x => x.Properties).ToArray());
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
                throw new Exception($"Name is already set in the AddCommand function, do not add a name in the builder");
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
            commandRegistry.AddCommand(new ConnectionCommands.ConnectCommand());
            commandRegistry.AddCommand(new ConnectionCommands.ReConnectCommand());
            commandRegistry.AddCommand(new ConnectionCommands.DisconnectCommand());
            commandRegistry.AddCommand(new UserAssignmentCommands.AssignUserCommand());
            commandRegistry.AddCommand(new UserAssignmentCommands.UnAssignUserCommand());
            commandRegistry.AddCommand(new HintCommands.ShowHintsCommand());
            commandRegistry.AddCommand(new AppSettingManagementCommands.PrintAppSettingsCommand());
            commandRegistry.AddCommand(new AppSettingManagementCommands.ToggleAppSettings());
            commandRegistry.AddCommand(new AppSettingManagementCommands.EditTagIgnoreList());
            commandRegistry.AddCommand(new AuxiliarySessionCommands.AddAuxiliarySessionsCommand());
            commandRegistry.AddCommand(new AuxiliarySessionCommands.RemoveAuxiliarySessionsCommand());
            commandRegistry.AddCommand(new AuxiliarySessionCommands.SendAsAuxiliarySession());
            if (Debugger.IsAttached)
            {
                commandRegistry.AddCommand(new DevCommands.DevGoalCommand());
            }
        }
    }
}
