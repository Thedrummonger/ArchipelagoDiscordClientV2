using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Text;
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
                if (!itemSendMessage.Item.Flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement)) return [];
                if (itemSendMessage.Receiver.Slot == itemSendMessage.Sender.Slot) return [];
                foreach (var i in session.Settings.SlotAssociations)
                {
                    if (!i.Value.Contains(itemSendMessage.Receiver.Name)) { continue; }
                    if (i.Value.Contains(itemSendMessage.Sender.Name)) { continue; } //Same player, different slot
                    ToPing.Add(i.Key);
                }
                return ToPing;
            }
            HashSet<ulong> GetHintPing(HintItemSendLogMessage hintItemSendLog)
            {
                HashSet<ulong> ToPing = [];
                if (hintItemSendLog.Receiver.Slot == hintItemSendLog.Sender.Slot) return [];
                if (hintItemSendLog.IsFound) return [];
                foreach (var i in session.Settings.SlotAssociations)
                {
                    if (!i.Value.Contains(hintItemSendLog.Sender.Name)) { continue; }
                    if (i.Value.Contains(hintItemSendLog.Receiver.Name)) { continue; } //Same player, different slot
                    ToPing.Add(i.Key);
                }
                return ToPing;
            }

        }
        public static string ColorLogMessage(this LogMessage message)
        {
            StringBuilder FormattedMessage = new StringBuilder();
            foreach (var part in message.Parts)
            {
                FormattedMessage.Append(part.Text.SetColor(part.Color));
            }
            return FormattedMessage.ToString();
        }

        public static bool ShouldRelayHintMessage(this HintItemSendLogMessage hintLogMessage, ActiveBotSession session)
        {
            HashSet<string> listeningPlayers = [.. session.AuxiliarySessions.Keys, session.ArchipelagoSession.Players.ActivePlayer.Name];
            // True if the active player is the receiver OR if the receiver is not in the active set
            return hintLogMessage.IsReceiverTheActivePlayer || !listeningPlayers.Contains(hintLogMessage.Receiver.Name);
        }

        public static bool ShouldIgnoreMessage(this LogMessage logMessage, ActiveBotSession session)
        {
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
        public static string[] GetTags(this LeaveLogMessage Message)
        {
            string MessageString = string.Join('\n', Message.Parts.Select(x => x.Text));

            string TagPattern = @"\[(.*?)\]"; //Tags are defined in brackets
            MatchCollection TagMatches = Regex.Matches(MessageString, TagPattern);
            HashSet<string> tags = [];
            if (TagMatches.Count > 0)
            {
                string lastMatch = TagMatches[^1].Groups[1].Value; //Tags are always at the end of a message
                string[] parts = lastMatch.Split(',');             //I don't think any brackets would ever appear other than the tags
                foreach (string part in parts)                     //but just pick the last instance of brackets to be safe
                {
                    string PartTrimmed = part.Trim();
                    if (!PartTrimmed.StartsWith("'") || !PartTrimmed.EndsWith("'")) { continue; } //Probably more unnecessary checking
                    string Tag = PartTrimmed[1..^1];                                              //But tags are always in single quotes
                    tags.Add(Tag);
                }
            }

            return [.. tags];
        }
    }
}
