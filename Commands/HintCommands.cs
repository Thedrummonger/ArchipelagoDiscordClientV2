using Archipelago.MultiClient.Net.Enums;
using ArchipelagoDiscordClient;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueue;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class HintCommands
    {
        public static async Task HandleShowHintsCommand(SocketSlashCommand command, DiscordBot discordBot)
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

            Console.WriteLine($"Showing hints for slot {session.archipelagoSession.ConnectionInfo.Slot}");

            var hints = session.archipelagoSession.DataStorage.GetHints();
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

                FindingPlayerName = FindingPlayer.Slot == session.archipelagoSession.ConnectionInfo.Slot ?
                    FindingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Magenta) :
                    FindingPlayerName.SetColor(Archipelago.MultiClient.Net.Models.Color.Yellow);

                ReceivingPlayerName = ReceivingPlayer.Slot == session.archipelagoSession.ConnectionInfo.Slot ?
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

            await command.RespondAsync($"Hints for {session.archipelagoSession.Players.GetPlayerName(session.archipelagoSession.ConnectionInfo.Slot)}", ephemeral: false);
            foreach (var i in Messages)
            {
                MessageQueue.QueueMessage(i, discordBot);
            }
        }
    }
}
