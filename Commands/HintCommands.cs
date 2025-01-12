using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Constants;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueueData;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class HintCommands
    {
        public class ShowHintsCommand : ICommand
        {
            public string Name => "show_hints";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Shows all hint for the current player")
                    .AddOption("player", ApplicationCommandOptionType.String, "Player to get Hints for, defaults to connected player", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var TargetPlayer = session!.archipelagoSession.Players.ActivePlayer;
                var PlayerNameArg = Data.GetArg("player")?.Value;
                if (PlayerNameArg is string PlayerNameString)
                {
                    TargetPlayer = session.archipelagoSession.Players.AllPlayers.FirstOrDefault(x => x.Name == PlayerNameString);
                }

                if (TargetPlayer == null)
                {
                    await command.RespondAsync($"{PlayerNameArg} was not a valid player", ephemeral: true);
                    return;
                }

                Console.WriteLine($"Showing hints for player {TargetPlayer.Name} [{TargetPlayer.Slot}]");

                var hints = session.archipelagoSession.DataStorage.GetHints(TargetPlayer.Slot);
                Console.WriteLine($"{hints.Length} Found");
                List<QueuedMessage> Messages = [];
                foreach (var hint in hints)
                {
                    var FindingPlayer = session.archipelagoSession.Players.GetPlayerInfo(hint.FindingPlayer);
                    var ReceivingPlayer = session.archipelagoSession.Players.GetPlayerInfo(hint.ReceivingPlayer);
                    var Item = session.archipelagoSession.Items.GetItemName(hint.ItemId, ReceivingPlayer.Game);
                    var Location = session.archipelagoSession.Locations.GetLocationNameFromId(hint.LocationId, FindingPlayer.Game);
                    var Entrance = hint.Entrance;

                    var FindingPlayerName = FindingPlayer.Name;
                    var ReceivingPlayerName = ReceivingPlayer.Name;

                    var FoundString = hint.Found ?
                        ColorHelpers.SetColor("Found", Colors.Hints.found) :
                        ColorHelpers.SetColor("Not Found", Colors.Hints.Unfound);

                    if (hint.ItemFlags.HasFlag(ItemFlags.Advancement)) { Item = Item.SetColor(Colors.Items.Progression); }
                    else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Item.SetColor(Colors.Items.Important); }
                    else if (hint.ItemFlags.HasFlag(ItemFlags.Trap)) { Item = Item.SetColor(Colors.Items.Traps); }
                    else { Item = Item.SetColor(Colors.Items.Normal); }

                    Location = Location.SetColor(Colors.Location);
                    var EntranceLine = Entrance.IsNullOrWhiteSpace() ? "" :
                        $" at {Entrance.SetColor(Colors.Entrance)}";

                    FindingPlayerName = FindingPlayer.Slot == TargetPlayer.Slot ?
                        FindingPlayerName.SetColor(Colors.Players.Local) :
                        FindingPlayerName.SetColor(Colors.Players.Other);

                    ReceivingPlayerName = ReceivingPlayer.Slot == TargetPlayer.Slot ?
                        ReceivingPlayerName.SetColor(Colors.Players.Local) :
                        ReceivingPlayerName.SetColor(Colors.Players.Other);
                    string HintLine = $"{FindingPlayerName} has {Item} at {Location} for {ReceivingPlayerName} {EntranceLine}({FoundString})";

                    Messages.Add(Data.socketTextChannel!.CreateSimpleQueuedMessage(HintLine));
                }
                if (Messages.Count < 1)
                {
                    await command.RespondAsync("No hints available for this slot.", ephemeral: true);
                    return;
                }

                await command.RespondAsync($"Hints for {TargetPlayer.Name} playing {TargetPlayer.Game}", ephemeral: false);
                foreach (var i in Messages)
                {
                    Console.WriteLine($"Queueing {i.Message}");
                    MessageQueueData.QueueMessage(i, discordBot);
                }
            }
        }
    }
}
