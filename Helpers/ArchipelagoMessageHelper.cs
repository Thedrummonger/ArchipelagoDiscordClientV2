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
            switch (message)
            {
                case HintItemSendLogMessage hintItemSendLog:
                    return GetHintPing(hintItemSendLog);
                case ItemSendLogMessage itemSendMessage:
                    return GetItemSendPings(itemSendMessage);
                default:
                    return [];
            }
            HashSet<ulong> GetItemSendPings(ItemSendLogMessage itemSendMessage)
            {
                HashSet<ulong> ToPing = [];
                if (!itemSendMessage.Item.Flags.HasFlag(Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement)) return [];
                if (itemSendMessage.Receiver.Slot == itemSendMessage.Sender.Slot) return [];
                foreach (var i in session.settings.SlotAssociations)
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
                foreach (var i in session.settings.SlotAssociations)
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
            //We can assume this is always true since the message is only sent to the sender and receiver
            //if (!hintLogMessage.IsRelatedToActivePlayer) return false;

            //A list of players that would print this hint message when it is received
            HashSet<string> ListeningPlayers = [session.archipelagoSession.Players.ActivePlayer.Name, .. session.AuxiliarySessions.Keys];

            //The slot receiving the message should always take priority
            if (hintLogMessage.IsReceiverTheActivePlayer) return true;

            //At this point we know we are the sender of the item

            //If the receiver of the item is not an active slot in the session, we should send the hint
            if (!ListeningPlayers.Contains(hintLogMessage.Receiver.Name)) return true;

            //This should only return false if We are the sender, but the receiver is an active slot in the session
            return false;
        }

        public static bool ShouldIgnoreMessage(this LogMessage logMessage, ActiveBotSession session)
        {
            switch (logMessage)
            {
                case ServerChatLogMessage:
                    return session.settings.IgnoreChats;
                case ChatLogMessage message:
                    //If it's a discord message from the active player, assume it came from the same chat and doesn't need to be posted
                    if (message.IsActivePlayer && message.ToString().Contains(": [Discord:")) { return true; }
                    return session.settings.IgnoreChats || message.ShouldIgnoreConnectedPlayerChat(session);

                case JoinLogMessage message:
                    return session.settings.IgnoreLeaveJoin || session.settings.IgnoreTags.Intersect(message.Tags.Select(x => x.ToLower())).Any();
                case LeaveLogMessage message:
                    return session.settings.IgnoreLeaveJoin || session.settings.IgnoreTags.Intersect(message.GetTags().Select(x => x.ToLower())).Any();

                case HintItemSendLogMessage message:
                    return session.settings.IgnoreHints || !message.ShouldRelayHintMessage(session) || logMessage.ShouldIgnoreUnrelated(session);

                case ItemCheatLogMessage:
                case ItemSendLogMessage:
                    return session.settings.IgnoreItemSend || logMessage.ShouldIgnoreUnrelated(session);

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
            if (!session.settings.IgnoreConnectedPlayerChats) { return false; }
            if (logMessage.Player.Name == session.archipelagoSession.Players.ActivePlayer.Name) return true;
            if (logMessage.Player.Name.In([.. session.AuxiliarySessions.Keys])) return true;
            return false;
        }

        public static bool ShouldIgnoreUnrelated(this LogMessage logMessage, ActiveBotSession session)
        {
            if (!session.settings.IgnoreUnrelated) { return false; }
            switch (logMessage)
            {
                case PlayerSpecificLogMessage playerSpecificLogMessage:
                    if (playerSpecificLogMessage.Player.Name == session.archipelagoSession.Players.ActivePlayer.Name) { return false; }
                    if (playerSpecificLogMessage.Player.Name.In([.. session.AuxiliarySessions.Keys])) { return false; }
                    break;
                case ItemSendLogMessage ItemSendMessage:
                    if (ItemSendMessage.Sender.Name == session.archipelagoSession.Players.ActivePlayer.Name) { return false; }
                    if (ItemSendMessage.Receiver.Name == session.archipelagoSession.Players.ActivePlayer.Name) { return false; }
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
