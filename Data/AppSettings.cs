using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public class AppSettings
    {
        public string BotToken = "";
        public int DiscordRateLimitDelay = 500;
        public HashSet<string> IgnoreTags = ["tracker"];
        public bool IgnoreLeaveJoin = false;
        public bool IgnoreItemSend = false;
        public bool IgnoreChats = false;
    }
}
