using System.Diagnostics;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    /// <summary>
    /// Manages the registration and retrieval of commands for the Discord bot.
    /// </summary>
    public class CommandRegistry
    {
        private DiscordBot discordBot;
        private Dictionary<string, ICommand> Commands = [];
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandRegistry"/> class and registers commands.
        /// </summary>
        /// <param name="bot">The Discord bot instance managing commands.</param>
        public CommandRegistry(DiscordBot bot)
        {
            discordBot = bot;
            CreateCommands();
        }
        /// <summary>
        /// Adds a command to the registry.
        /// </summary>
        /// <param name="command">The command to register.</param>
        public void AddCommand(ICommand command)
        {
            Commands[command.Name] = command;
        }
        /// <summary>
        /// Retrieves a registered command by its name.
        /// </summary>
        /// <param name="Name">The name of the command.</param>
        /// <returns>The registered command if found, otherwise null.</returns>
        public ICommand? GetCommand(string Name)
        {
            if (!Commands.TryGetValue(Name, out ICommand? value)) { return null; }
            return value;
        }
        /// <summary>
        /// Initializes the command registry by overwriting global application commands in Discord.
        /// </summary>
        public async Task RegisterSlashCommands()
        {
            await discordBot.Client.BulkOverwriteGlobalApplicationCommandsAsync(Commands.Values.Select(x => x.Properties).ToArray());
        }
        /// <summary>
        /// Scans the application for all classes implementing <see cref="ICommand"/> and registers them.
        /// </summary>
        public void CreateCommands()
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
                    AddCommand(command);
                }
            }
        }
    }
}
