using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDiscordClientLegacy.Data;
using System.Text.RegularExpressions;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class ArchipelagoMessageHelper
    {
        /// <summary>
        /// Determines which Discord users should be pinged based on the contents of a log message.
        /// </summary>
        /// <param name="message">The log message being processed.</param>
        /// <param name="session">The active bot session containing slot associations.</param>
        /// <returns>A set of Discord user IDs to ping.</returns>
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
        /// <summary>
        /// Converts a log message into a colored string based on predefined ANSI color codes.
        /// </summary>
        /// <param name="message">The log message to format.</param>
        /// <returns>A string with color formatting applied.</returns>
        public static string ToColoredString(this LogMessage message)
        {
            return string.Concat(message.Parts.Select(part => part.Text.SetColor(part.Color)));
        }

        /// <summary>
        /// Determines whether a hint message should be relayed based on active listening players.
        /// </summary>
        /// <param name="hintLogMessage">The hint log message.</param>
        /// <param name="session">The active bot session.</param>
        /// <returns>True if the message should be relayed, otherwise false.</returns>
        public static bool ShouldRelayHintMessage(this HintItemSendLogMessage hintLogMessage, ActiveBotSession session)
        {
            //Get all slots that are actively listening for hint messages in this channel.
            HashSet<string> listeningPlayers = [.. session.AuxiliarySessions.Keys, session.ArchipelagoSession.Players.ActivePlayer.Name];
            //True if the active player is the receiver OR if the receiver is not actively listening for hint messages in this channel.
            return hintLogMessage.IsReceiverTheActivePlayer || !listeningPlayers.Contains(hintLogMessage.Receiver.Name);
        }

        /// <summary>
        /// Determines whether a log message should be ignored based on session settings.
        /// </summary>
        /// <param name="logMessage">The log message to evaluate.</param>
        /// <param name="session">The active bot session.</param>
        /// <returns>True if the message should be ignored, otherwise false.</returns>
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

        /// <summary>
        /// Determines whether chat messages from connected players should be ignored.
        /// </summary>
        /// <param name="logMessage">The chat log message.</param>
        /// <param name="session">The active bot session.</param>
        /// <returns>True if the message should be ignored, otherwise false.</returns>
        public static bool ShouldIgnoreConnectedPlayerChat(this ChatLogMessage logMessage, ActiveBotSession session)
        {
            if (!session.Settings.IgnoreConnectedPlayerChats) { return false; }
            if (logMessage.Player.Name == session.ArchipelagoSession.Players.ActivePlayer.Name) return true;
            if (logMessage.Player.Name.In([.. session.AuxiliarySessions.Keys])) return true;
            return false;
        }

        /// <summary>
        /// Determines whether a log message should be ignored because it is unrelated to the active session.
        /// </summary>
        /// <param name="logMessage">The log message.</param>
        /// <param name="session">The active bot session.</param>
        /// <returns>True if the message should be ignored, otherwise false.</returns>
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

        /// <summary>
        /// Extracts tags from a LeaveLogMessage, since this message type does not natively store tags.
        /// </summary>
        /// <param name="message">The leave log message.</param>
        /// <returns>A set of extracted tags.</returns>
        public static HashSet<string> GetTags(this LeaveLogMessage Message)
        {
            string messageString = string.Join('\n', Message.Parts.Select(x => x.Text));
            //Tags should be in the last instance of brackets in the LeaveLogMessage
            Match? match = Regex.Matches(messageString, @"\[(.*?)\]").LastOrDefault();
            if (match is null) return [];
            var rawTags = match.Groups[1].Value.TrimSplit(",");
            //Remove single quotes if present, otherwise return as-is.
            var processedTags = rawTags.Select(part => part.StartsWith("'") && part.EndsWith("'") ? part[1..^1] : part);
            return processedTags.ToHashSet();
        }

        public static MessageQueueData.IQueuedMessage FormatLogMessage(this LogMessage message, ActiveBotSession botSession)
        {
            return message switch
            {
                ItemSendLogMessage => new MessageQueueData.QueuedItemLogMessage(message.ToColoredString(), message.ToString(), message.GetUserPings(botSession)),
                JoinLogMessage => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).WithColor(Discord.Color.Green).Build()),
                LeaveLogMessage => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).WithColor(Discord.Color.Red).Build()),
                CollectLogMessage => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).WithColor(Discord.Color.Blue).Build()),
                ReleaseLogMessage => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).WithColor(Discord.Color.Blue).Build()),
                GoalLogMessage => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).WithColor(Discord.Color.Gold).Build()),
                ChatLogMessage or ServerChatLogMessage => new MessageQueueData.QueuedMessage(message.ToString()),
                CommandResultLogMessage => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).Build()),
                _ => new MessageQueueData.QueuedMessage(new Discord.EmbedBuilder().WithDescription(message.ToString()).Build()),
            };
        }
    }
}
