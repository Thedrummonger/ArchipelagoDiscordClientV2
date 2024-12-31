using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDiscordClientLegacy.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                case ChatLogMessage:
                    return bot.appSettings.IgnoreChats;

                case JoinLogMessage message:
                    return bot.appSettings.IgnoreLeaveJoin || bot.appSettings.IgnoreTags.Intersect(message.Tags).Any();
                case LeaveLogMessage message:
                    return bot.appSettings.IgnoreLeaveJoin;

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
    }
}
