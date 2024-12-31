using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Text.RegularExpressions;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class ArchipelagoMessageHelper
    {
        public static bool ShouldIgnoreMessage(this LogMessage logMessage, DiscordBot bot)
        {
            switch (logMessage)
            {
                case ServerChatLogMessage:
                    return bot.appSettings.IgnoreChats;
                case ChatLogMessage message:
                    //If it's a discord message from the active player, assume it came from the same chat and doesn't need to be posted
                    if (message.IsActivePlayer && message.ToString().Contains(": [Discord:")) { return true; }
                    return bot.appSettings.IgnoreChats || (message.IsActivePlayer && bot.appSettings.IgnoreConnectedPlayerChats);

                case JoinLogMessage message:
                    return bot.appSettings.IgnoreLeaveJoin || bot.appSettings.IgnoreTags.Intersect(message.Tags.Select(x => x.ToLower())).Any();
                case LeaveLogMessage message:
                    return bot.appSettings.IgnoreLeaveJoin || bot.appSettings.IgnoreTags.Intersect(message.GetTags().Select(x => x.ToLower())).Any();

                case HintItemSendLogMessage:
                case ItemCheatLogMessage:
                case ItemSendLogMessage:
                    return bot.appSettings.IgnoreItemSend;

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
