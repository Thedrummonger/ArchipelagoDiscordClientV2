using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Text.RegularExpressions;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class ArchipelagoMessageHelper
    {
        public static HashSet<ulong> GetUserPings(this LogMessage message, ActiveBotSession session)
        {
            return message switch
            {
                HintItemSendLogMessage hintItemSendLog => GetHintPing(hintItemSendLog),
                ItemSendLogMessage itemSendMessage => GetItemSendPings(itemSendMessage),
                _ => [],
            };
            HashSet<ulong> GetItemSendPings(ItemSendLogMessage itemSendMessage)
            {
                HashSet<ulong> ToPing = [];
                //Only ping for Progression Items
                if (!itemSendMessage.Item.Flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement)) return [];
                //If a player sends an item to themselves, no need to notify
                if (itemSendMessage.Receiver.Slot == itemSendMessage.Sender.Slot) return [];
                //Get all discord users associated with the receiving slot
                var receiverAssociations = session.Settings.SlotAssociations.Where(kvp => kvp.Value.Contains(itemSendMessage.Receiver.Name));
                //If the receiver and sender slot are associated with the same discord user, no need to notify
                var validAssociations = receiverAssociations.Where(kvp => !kvp.Value.Contains(itemSendMessage.Sender.Name));
                return validAssociations.Select(kvp => kvp.Key).ToHashSet();
            }
            HashSet<ulong> GetHintPing(HintItemSendLogMessage hintItemSendLog)
            {
                HashSet<ulong> ToPing = [];
                //Only ping for Items that have not been found
                if (hintItemSendLog.IsFound) return [];
                //If a player sends an item to themselves, no need to notify
                if (hintItemSendLog.Receiver.Slot == hintItemSendLog.Sender.Slot) return [];
                //Get all discord users associated with sending slot
                var senderAssociations = session.Settings.SlotAssociations.Where(kvp => kvp.Value.Contains(hintItemSendLog.Sender.Name));
                //If the receiver and sender slot are associated with the same player, no need to notify that player
                var finalAssociations = senderAssociations.Where(kvp => !kvp.Value.Contains(hintItemSendLog.Receiver.Name));
                return finalAssociations.Select(kvp => kvp.Key).ToHashSet();
            }

        }
        public static string ToColoredString(this LogMessage message)
        {
            return string.Concat(message.Parts.Select(part => part.Text.SetColor(part.Color)));
        }

        public static bool ShouldRelayHintMessage(this HintItemSendLogMessage hintLogMessage, ActiveBotSession session)
        {
            //Get all slots that are actively listening for hint messages in this channel.
            HashSet<string> listeningPlayers = [.. session.AuxiliarySessions.Keys, session.ArchipelagoSession.Players.ActivePlayer.Name];
            //True if the active player is the receiver OR if the receiver is not actively listening for hint messages in this channel.
            return hintLogMessage.IsReceiverTheActivePlayer || !listeningPlayers.Contains(hintLogMessage.Receiver.Name);
        }

        public static bool ShouldIgnoreMessage(this LogMessage logMessage, ActiveBotSession session)
        {
            if (string.IsNullOrWhiteSpace(logMessage.ToString()))
                return true;
            switch (logMessage)
            {
                case ServerChatLogMessage:
                    return session.Settings.IgnoreChats;
                case ChatLogMessage message:
                    //If it's a discord message from the active player, assume it came from the same chat and doesn't need to be posted
                    if (message.IsActivePlayer && message.ToString().Contains(": [Discord:")) { return true; }
                    return session.Settings.IgnoreChats || message.ShouldIgnoreConnectedPlayerChat(session);

                case JoinLogMessage message:
                    return session.Settings.IgnoreLeaveJoin || session.Settings.IgnoreTags.Intersect(message.Tags.Select(x => x.ToLower())).Any();
                case LeaveLogMessage message:
                    return session.Settings.IgnoreLeaveJoin || session.Settings.IgnoreTags.Intersect(message.GetTags().Select(x => x.ToLower())).Any();

                case HintItemSendLogMessage message:
                    return session.Settings.IgnoreHints || !message.ShouldRelayHintMessage(session) || logMessage.ShouldIgnoreUnrelated(session);

                case ItemCheatLogMessage:
                case ItemSendLogMessage:
                    return session.Settings.IgnoreItemSend || logMessage.ShouldIgnoreUnrelated(session);

                case AdminCommandResultLogMessage:
                case GoalLogMessage:
                case ReleaseLogMessage:
                case TagsChangedLogMessage:
                case TutorialLogMessage:
                case CommandResultLogMessage:
                case CollectLogMessage:
                case PlayerSpecificLogMessage:
                default:
                    return false;
            };
        }

        public static bool ShouldIgnoreConnectedPlayerChat(this ChatLogMessage logMessage, ActiveBotSession session)
        {
            if (!session.Settings.IgnoreConnectedPlayerChats) { return false; }
            if (logMessage.Player.Name == session.ArchipelagoSession.Players.ActivePlayer.Name) return true;
            if (logMessage.Player.Name.In([.. session.AuxiliarySessions.Keys])) return true;
            return false;
        }

        public static bool ShouldIgnoreUnrelated(this LogMessage logMessage, ActiveBotSession session)
        {
            if (!session.Settings.IgnoreUnrelated) { return false; }
            switch (logMessage)
            {
                case PlayerSpecificLogMessage playerSpecificLogMessage:
                    if (playerSpecificLogMessage.Player.Name == session.ArchipelagoSession.Players.ActivePlayer.Name) { return false; }
                    if (playerSpecificLogMessage.Player.Name.In([.. session.AuxiliarySessions.Keys])) { return false; }
                    break;
                case ItemSendLogMessage ItemSendMessage:
                    if (ItemSendMessage.Sender.Name == session.ArchipelagoSession.Players.ActivePlayer.Name) { return false; }
                    if (ItemSendMessage.Receiver.Name == session.ArchipelagoSession.Players.ActivePlayer.Name) { return false; }
                    if (ItemSendMessage.Sender.Name.In([.. session.AuxiliarySessions.Keys])) { return false; }
                    if (ItemSendMessage.Receiver.Name.In([.. session.AuxiliarySessions.Keys])) { return false; }
                    break;
            }
            return true;
        }

        //For some reason, LeaveLogMessage does not contain tags. For now we can extract the tags manually from the message.
        public static HashSet<string> GetTags(this LeaveLogMessage Message)
        {
            string messageString = string.Join('\n', Message.Parts.Select(x => x.Text));
            //Tags should be in the last instance of brackets in the LeaveLogMessage
            Match? match = Regex.Matches(messageString, @"\[(.*?)\]").LastOrDefault();
            if (match is null) return [];
            var rawTags = match.Groups[1].Value.TrimSplit(",");
            //To my knowledge tags always are always in single quotes,
            //if they are we need to remove those, but lets also handle if they aren't for some reason
            var processedTags = rawTags.Select(part => part.StartsWith("'") && part.EndsWith("'") ? part[1..^1] : part);
            return processedTags.ToHashSet();
        }
    }
}
