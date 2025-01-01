﻿using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
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
                var Data = command.GetCommandData();
                if (Data.socketTextChannel is null)
                {
                    await command.RespondAsync("Only Text Channels are Supported", ephemeral: true);
                    return;
                }

                // Check if the guild and channel have an active session
                if (!discordBot.ActiveSessions.TryGetValue(Data.channelId, out var session))
                {
                    await command.RespondAsync("This channel is not connected to any Archipelago session.", ephemeral: true);
                    return;
                }

                var TargetPlayer = session.archipelagoSession.Players.ActivePlayer;
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

                    var FindingPlayerName = FindingPlayer.Name;
                    var ReceivingPlayerName = ReceivingPlayer.Name;

                    var FoundString = hint.Found ?
                        ColorHelpers.SetColor("Found", Archipelago.MultiClient.Net.Models.Color.Green) :
                        ColorHelpers.SetColor("Not Found", Archipelago.MultiClient.Net.Models.Color.Red);

                    if (hint.ItemFlags.HasFlag(ItemFlags.Advancement)) { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.Plum); }
                    else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.SlateBlue); }
                    else if (hint.ItemFlags.HasFlag(ItemFlags.NeverExclude)) { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.Salmon); }
                    else { Item = Item.SetColor(Archipelago.MultiClient.Net.Models.Color.Cyan); }

                    Location = Location.SetColor(Archipelago.MultiClient.Net.Models.Color.Green);

                    FindingPlayerName = FindingPlayer.Slot == TargetPlayer.Slot ?
                        FindingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Magenta) :
                        FindingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Yellow);

                    ReceivingPlayerName = ReceivingPlayer.Slot == TargetPlayer.Slot ?
                        ReceivingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Magenta) :
                        ReceivingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Yellow);
                    string HintLine = $"{FindingPlayerName} has {Item} at {Location} for {ReceivingPlayerName} ({FoundString})";

                    Messages.Add(Data.socketTextChannel.CreateSimpleQueuedMessage(HintLine));
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
