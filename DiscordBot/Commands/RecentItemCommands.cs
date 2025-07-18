using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    class RecentItemCommands
    {
        public class ShowRecentItemCommand : ICommand
        {
            public string Name => "recent";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .AddOption("slot", ApplicationCommandOptionType.String, "Player to show recent items for", false)
                .AddOption("only_progression", ApplicationCommandOptionType.Boolean, "Only Show Progression Items. Default true", false)
                .WithDescription("Prints all items obtained by this slot since this command was last run").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string Result))
                {
                    await command.RespondAsync(Result, ephemeral: true);
                    return;
                }
                string SlotArg = commandData.GetArg("slot")?.GetValue<string>() ?? session!.ArchipelagoSession.Players.ActivePlayer.Name;
                bool ProgArg = commandData.GetArg("only_progression")?.GetValue<bool>() ?? true;

                var RecentItems = RecentItemTrackingHelper.GetRelevantMessages(session!, SlotArg, ProgArg);
                if (RecentItems.Length == 0)
                {
                    await command.RespondAsync($"{SlotArg} has not received any items since last running this command!", ephemeral: true);
                    return;
                }
                await command.RespondAsync($"Recent items for {SlotArg}");
                foreach (var i in RecentItems)
                {
                    session!.QueueMessageForChannel(i);
                }
            }
        }
    }
}
