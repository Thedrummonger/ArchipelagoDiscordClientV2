using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoDiscordClientLegacy.Data;
using System.Linq;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueueData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public class RecentItemTrackingHelper
    {
        public static void CacheItemSendMessage(Sessions.ActiveBotSession botSession, LogMessage message)
        {
            if (message is not ItemSendLogMessage ItemLogMessage || message is HintItemSendLogMessage)
                return;

            try { botSession.RecentItems.Add(ItemLogMessage); }
            catch { }
        }
        public static QueuedItemLogMessage[] GetRelevantMessages(Sessions.ActiveBotSession botSession, string slotName, bool priorityOnly)
        {
            try
            {
                if (botSession.RecentItems.Count == 0) 
                    return [];

                List<ItemSendLogMessage> RecentItemLogMessages = [.. botSession.RecentItems];
                List<(ItemSendLogMessage Source, QueuedItemLogMessage Msg)> itemsToReport = [];

                foreach (var msg in RecentItemLogMessages)
                {
                    if (priorityOnly && !msg.Item.Flags.HasFlag(ItemFlags.Advancement)) 
                        continue;
                    if (msg.Receiver.Name != slotName) 
                        continue;
                    itemsToReport.Add((msg, new QueuedItemLogMessage(ColorRecentItemString(msg.Item, msg.Receiver), msg.ToString())));
                }

                if (itemsToReport.Count == 0) return [];

                var toRemove = new HashSet<ItemSendLogMessage>(itemsToReport.Select(t => t.Source));
                botSession.RecentItems.RemoveAll(toRemove.Contains);

                return [.. itemsToReport.OrderBy(x => x.Source.Item.ItemName, StringComparer.OrdinalIgnoreCase).Select(x => x.Msg)];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get messages. {ex}");
                return [];
            }
        }

        public static string ColorRecentItemString(ItemInfo item, PlayerInfo ActivePlayer)
        {
            string Item = item.GetColoredString();

            string FindingPlayerName = item.Player.GetColoredString(ActivePlayer.Name);

            string FinalMessage = $"{Item} from {FindingPlayerName}";
            if (!string.IsNullOrWhiteSpace(item.LocationName))
                FinalMessage += $" ({item.LocationName.SetColor(ColorHelpers.Locations.Location)})";

            return FinalMessage;

        }
    }
}
