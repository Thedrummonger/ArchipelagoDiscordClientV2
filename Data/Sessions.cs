﻿using Archipelago.MultiClient.Net;
using Discord.WebSocket;
using TDMUtils;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Sessions
    {
        public class ActiveBotSession(SessionSetting DefaultSettings)
        {
            public required SocketTextChannel DiscordChannel;
            public required ArchipelagoSession archipelagoSession;
            public string? OriginalChannelName = null;
            public SessionSetting settings = DefaultSettings.DeepClone();
        }

        public class ArchipelagoConnectionInfo
        {
            public required string? IP { get; set; }
            public required int Port { get; set; }
            public required string? Game { get; set; }
            public required string? Name { get; set; }
            public required string? Password { get; set; }
        }

        public class SessionContructor
        {
            public ArchipelagoConnectionInfo? archipelagoConnectionInfo {  get; set; }
            public SessionSetting? Settings { get; set; }
        }
    }
}
