using Archipelago.MultiClient.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Sessions
    {
        public class ActiveBotSession
        {
            public SocketTextChannel DiscordChannel;
            public ArchipelagoSession archipelagoSession;
        }
    }
}
