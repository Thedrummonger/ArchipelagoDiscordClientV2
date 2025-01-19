using ArchipelagoDiscordClientLegacy.Commands.ConsoleCommands;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Handlers
{
    internal class ConsoleCommandHandlers
    {
        public static Dictionary<string, IConsoleCommand> ConsoleCommandRegistry = [];
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
        public static void RunUserInputLoop(DiscordBot botClient)
        {
            while (true)
            {
                var input = Console.ReadLine();
                if (input is null) continue;
                if (ShouldExit(input)) break;
                ProcessConsoleCommand(botClient, input);
            }
        }

        private static bool ShouldExit(string input)
        {
            if (input is null || input != "exit") return false;
            //Maybe do a confirmation?
            return true;
        }

        public static void ProcessConsoleCommand(DiscordBot botClient, string Input)
        {
            if (ConsoleCommandRegistry.TryGetValue(Input, out IConsoleCommand? consoleCommand))
            {
                consoleCommand.ExecuteCommand(botClient);
            }
            else
            {
                Console.WriteLine("Invalid Command");
            }
        }
    }
}
