using System.Diagnostics;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public class CommandRegistry
    {
        private DiscordBot _discordBot;
        private Dictionary<string, ICommand> Commands = [];
        public CommandRegistry(DiscordBot bot)
        {
            _discordBot = bot;
            var CommandCreation = new CommandCreation();
            CommandCreation.CreateCommands(this);
        }

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
            await _discordBot.Client.BulkOverwriteGlobalApplicationCommandsAsync(Commands.Values.Select(x => x.Properties).ToArray());
        }
    }

    class CommandCreation
    {
        public void CreateCommands(CommandRegistry commandRegistry)
        {
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ICommand).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToList();

            foreach (var commandType in commandTypes)
            {
                if (Activator.CreateInstance(commandType) is ICommand command && (!command.IsDebugCommand || Debugger.IsAttached))
                {
                    Console.WriteLine($"Registering Command: {command.Name}");
                    commandRegistry.AddCommand(command);
                }
            }
        }
    }
}
