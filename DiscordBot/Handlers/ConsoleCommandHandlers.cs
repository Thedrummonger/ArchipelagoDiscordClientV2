using ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    public class ConsoleCommandHandlers
    {
        public static Dictionary<string, IConsoleCommand> ConsoleCommandRegistry { get; } = [];
        /// <summary>
        /// Scans the application for all classes implementing <see cref="IConsoleCommand"/> 
        /// and registers them in the <see cref="ConsoleCommandRegistry"/>.
        /// </summary>
        public static void RegisterCommands()
        {
            var commandTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IConsoleCommand).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToList();

            foreach (var commandType in commandTypes)
            {
                if (Activator.CreateInstance(commandType) is IConsoleCommand command)
                {
                    Console.WriteLine($"Registering Console Command: {command.Name}");
                    ConsoleCommandRegistry[command.Name] = command;
                }
            }
        }
        /// <summary>
        /// Continuously listens for user input in the console and processes commands accordingly.
        /// </summary>
        /// <param name="botClient">The Discord bot instance that commands interact with.</param>
        public static void RunUserInputLoop(DiscordBot botClient)
        {
            RegisterCommands();
            while (true)
            {
                var input = Console.ReadLine();
                if (input is null) continue;
                if (ShouldExit(input)) break;
                ProcessConsoleCommand(botClient, input);
            }
        }
        /// <summary>
        /// Determines whether the user has entered an exit command.
        /// </summary>
        /// <param name="input">The user's console input.</param>
        /// <returns>True if the input is "exit", otherwise false.</returns>
        private static bool ShouldExit(string input)
        {
            if (input is null || input != "exit") return false;
            //Maybe do a confirmation?
            return true;
        }

        /// <summary>
        /// Processes a console command by executing the corresponding registered command.
        /// </summary>
        /// <param name="botClient">The Discord bot instance that commands interact with.</param>
        /// <param name="input">The user's console input.</param>
        public static void ProcessConsoleCommand(DiscordBot botClient, string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            var Parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (Parts.Length == 0) return;
            var Command = Parts[0];
            var Args = Parts.Skip(1).ToArray();

            if (ConsoleCommandRegistry.TryGetValue(Command, out var consoleCommand))
                consoleCommand.ExecuteCommand(botClient, Args);
            else
                Console.WriteLine($"Unknown Command: {Command}");
        }
    }
}
