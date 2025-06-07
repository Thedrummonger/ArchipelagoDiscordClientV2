using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueueData;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class HintCommands
    {
        public class HintCommand : ICommand
        {
            public string Name => "hint";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Sends an item hint command to the AP server")
                    .AddOption("slot", ApplicationCommandOptionType.String, "Player to hint as", false)
                    .AddOption("hint", ApplicationCommandOptionType.String, "Hinted item", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot) => await SendHint(command, discordBot, false);
        }
        public class HintLocationCommand : ICommand
        {
            public string Name => "hint_location";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Sends a location hint command to the AP server")
                    .AddOption("slot", ApplicationCommandOptionType.String, "Player to hint as, defaults to connected player", false)
                    .AddOption("hint", ApplicationCommandOptionType.String, "Hinted location", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot) => await SendHint(command, discordBot, false);
        }

        private static async Task SendHint(SocketSlashCommand command, DiscordBot discordBot, bool LocationHint)
        {
            if (!command.Validate(discordBot, out ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string Result))
            {
                await command.RespondAsync(Result, ephemeral: true);
                return;
            }
            var SlotArgs = commandData.GetArg("slot")?.GetValue<string>();
            var MessageArgs = commandData.GetArg("hint")?.GetValue<string>();

            ArchipelagoSession TargetSession;
            if (String.IsNullOrWhiteSpace(SlotArgs) || SlotArgs == session!.ArchipelagoSession.Players.ActivePlayer.Name)
                TargetSession = session!.ArchipelagoSession;
            else if (session!.AuxiliarySessions.TryGetValue(SlotArgs, out ArchipelagoSession? AuxiliarySession))
                TargetSession = AuxiliarySession;
            else
            {
                await command.RespondAsync("The given slot did not have an active auxiliary connection", ephemeral: true);
                return;
            }

            string Message = LocationHint ? "!hint_location" : "!hint";
            if (MessageArgs is string hintedItem)
                Message += $" {hintedItem}";

            await command.RespondAsync($"[{TargetSession.Players.ActivePlayer.Name}] {Message}", ephemeral: true);

            TargetSession.Say(Message);
        }

        public class ShowHintsCommand : ICommand
        {
            public string Name => "show_hints";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Shows all hint for the current player")
                    .AddOption("player", ApplicationCommandOptionType.String, "Player to get Hints for, defaults to connected player", false)
                    .AddOption("filter_found", ApplicationCommandOptionType.Boolean, "True: show only found, False: only not found. Leave blank for both", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out Sessions.ActiveBotSession? session, out CommandData.CommandDataModel Data, out string Error))
                {
                    await command.RespondAsync(Error, ephemeral: true);
                    return;
                }

                var TargetPlayer = session!.ArchipelagoSession.Players.ActivePlayer;
                var PlayerNameArg = Data.GetArg("player")?.Value;
                var filter = Data.GetArg("filter_found")?.GetValue<bool>();
                if (PlayerNameArg is string PlayerNameString)
                {
                    TargetPlayer = session.ArchipelagoSession.Players.AllPlayers.FirstOrDefault(x => x.Name == PlayerNameString);
                }

                if (TargetPlayer is null)
                {
                    await command.RespondAsync($"{PlayerNameArg} was not a valid player", ephemeral: true);
                    return;
                }

                Console.WriteLine($"Showing hints for player {TargetPlayer.Name} [{TargetPlayer.Slot}]");

                var hints = session.ArchipelagoSession.DataStorage.GetHints(TargetPlayer.Slot);
                Console.WriteLine($"{hints.Length} Found");
                List<QueuedItemLogMessage> Messages = [];
                foreach (var hint in hints)
                {
                    if (filter is not null && hint.Found != filter) continue;
                    var FindingPlayer = session.ArchipelagoSession.Players.GetPlayerInfo(hint.FindingPlayer);
                    var ReceivingPlayer = session.ArchipelagoSession.Players.GetPlayerInfo(hint.ReceivingPlayer);
                    var Item = session.ArchipelagoSession.Items.GetItemName(hint.ItemId, ReceivingPlayer.Game);
                    var Location = session.ArchipelagoSession.Locations.GetLocationNameFromId(hint.LocationId, FindingPlayer.Game);
                    var Entrance = hint.Entrance;

                    var FindingPlayerName = FindingPlayer.Name;
                    var ReceivingPlayerName = ReceivingPlayer.Name;

                    var FoundColor = hint.Status switch
                    {
                        HintStatus.Found => ColorHelpers.Hints.found,
                        HintStatus.Priority => ColorHelpers.Items.Progression,
                        HintStatus.Avoid => ColorHelpers.Items.Traps,
                        HintStatus.NoPriority => ColorHelpers.Items.Normal,
                        _ => Archipelago.MultiClient.Net.Models.Color.White,
                    };
                    var FoundString = ColorHelpers.SetColor(hint.Status.ToString(), FoundColor);

                    if (hint.ItemFlags.HasFlag(ItemFlags.Advancement)) { Item = Item.SetColor(ColorHelpers.Items.Progression); }
                    else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Item.SetColor(ColorHelpers.Items.Important); }
                    else if (hint.ItemFlags.HasFlag(ItemFlags.Trap)) { Item = Item.SetColor(ColorHelpers.Items.Traps); }
                    else { Item = Item.SetColor(ColorHelpers.Items.Normal); }

                    Location = Location.SetColor(ColorHelpers.Locations.Location);
                    var EntranceLine = Entrance.IsNullOrWhiteSpace() ? "" :
                        $" at {Entrance.SetColor(ColorHelpers.Locations.Entrance)}";

                    FindingPlayerName = FindingPlayer.Slot == TargetPlayer.Slot ?
                        FindingPlayerName.SetColor(ColorHelpers.Players.Local) :
                        FindingPlayerName.SetColor(ColorHelpers.Players.Other);

                    ReceivingPlayerName = ReceivingPlayer.Slot == TargetPlayer.Slot ?
                        ReceivingPlayerName.SetColor(ColorHelpers.Players.Local) :
                        ReceivingPlayerName.SetColor(ColorHelpers.Players.Other);
                    string HintLine = $"{ReceivingPlayerName}'s {Item} is at {Location} in {FindingPlayerName}'s world {EntranceLine}({FoundString})";

                    Messages.Add(new QueuedItemLogMessage(HintLine));
                }
                if (Messages.Count < 1)
                {
                    await command.RespondAsync("No hints available for this slot.", ephemeral: true);
                    return;
                }

                await command.RespondAsync($"Hints for {TargetPlayer.Name} playing {TargetPlayer.Game}", ephemeral: false);
                foreach (var i in Messages)
                {
                    session.QueueMessageForChannel(i);
                }
            }
        }
    }
}
