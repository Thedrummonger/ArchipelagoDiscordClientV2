using Archipelago.MultiClient.Net;
using ArchipelagoDiscordClientLegacy.Handlers;
using Discord.WebSocket;
using TDMUtils;

namespace ArchipelagoDiscordClientLegacy.Data
{
    public static class Sessions
    {
        /// <summary>
        /// Represents an active session of a Discord bot connected to an Archipelago server.
        /// </summary>
        public class ActiveBotSession
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ActiveBotSession"/> class.
            /// </summary>
            /// <param name="sessionConstructor">The session constructor containing connection settings.</param>
            /// <param name="parent">The parent Discord bot managing the session.</param>
            /// <param name="channel">The Discord channel associated with this session.</param>
            /// <param name="APSession">The main Archipelago session instance.</param>
            public ActiveBotSession(SessionConstructor sessionConstructor, DiscordBotData.DiscordBot parent, ISocketMessageChannel channel, ArchipelagoSession APSession)
            {
                Settings = sessionConstructor.Settings!.DeepClone();
                MessageQueue = new ActiveSessionMessageQueue(parent, this);
                ConnectionInfo = sessionConstructor.ArchipelagoConnectionInfo!.DeepClone();
                DiscordChannel = channel;
                ArchipelagoSession = APSession;
                AuxiliarySessions = [];
                ParentBot = parent;
            }
            public DiscordBotData.DiscordBot ParentBot { get; private set; }
            public ISocketMessageChannel DiscordChannel { get; private set; }
            public ArchipelagoSession ArchipelagoSession { get; private set; }
            public Dictionary<string, ArchipelagoSession> AuxiliarySessions { get; private set; }
            public SessionSetting Settings { get; private set; }
            public ArchipelagoConnectionInfo ConnectionInfo { get; private set; }
            public ActiveSessionMessageQueue MessageQueue { get; private set; }
            /// <summary>
            /// A dictionary that allows external code to store arbitrary data associated with this object.
            /// </summary>
            /// <remarks>
            /// This dictionary acts as an extensibility point, enabling external processes to attach custom data.
            /// </remarks>
            public Dictionary<string, object> Metadata { get; private set; } = [];
        }
        /// <summary>
        /// Stores connection details required to connect to an Archipelago server.
        /// </summary>
        public class ArchipelagoConnectionInfo
        {
            public required string? IP { get; set; }
            public required int Port { get; set; }
            public required string? Name { get; set; }
            public required string? Password { get; set; }
        }
        /// <summary>
        /// Represents the configuration needed to construct a new Archipelago session.
        /// </summary>
        public class SessionConstructor
        {
            public ArchipelagoConnectionInfo? ArchipelagoConnectionInfo { get; set; }
            public SessionSetting? Settings { get; set; }
            public HashSet<string> AuxiliarySessions { get; set; } = [];
        }
    }
}
