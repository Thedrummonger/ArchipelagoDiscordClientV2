using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using ArchipelagoDiscordClientLegacy.Data;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;

namespace ArchipelagoDiscordClientLegacy.Helpers
{
    public static class ArchipelagoConnectionHelpers
    {
        /// <summary>
        /// Event triggered when CleanAndCloseChannel is about to start.
        /// </summary>
        public static event Action<Sessions.ActiveBotSession>? OnChannelClosing;
        /// <summary>
        /// Event triggered when CleanAndCloseChannel has completed.
        /// </summary>
        public static event Action<Sessions.ActiveBotSession>? OnChannelClosed;
        /// <summary>
        /// Event triggered when a new session is successfully created.
        /// </summary>
        public static event Action<Sessions.ActiveBotSession>? OnSessionCreated;
        /// <summary>
        /// Event triggered when new Auxiliary sessions are successfully created.
        /// </summary>
        public static event Action<Sessions.ActiveBotSession, IEnumerable<string>>? OnAuxSessionCreated;
        /// <summary>
        /// Event triggered when Auxiliary sessions are closed.
        /// </summary>
        public static event Action<Sessions.ActiveBotSession, IEnumerable<string>>? OnAuxSessionClosed;

        /// <summary>
        /// Cleans up and closes an active Archipelago session associated with a specific Discord channel.
        /// </summary>
        /// <param name="bot">The Discord bot managing the session.</param>
        /// <param name="channelId">The Discord channel ID linked to the session.</param>
        public static async Task CleanAndCloseChannel(this DiscordBot bot, ulong channelId)
        {
            if (!bot.ActiveSessions.TryGetValue(channelId, out var session)) { return; }

            Console.WriteLine($"Disconnecting Channel {session.DiscordChannel.Name} from server {session.ArchipelagoSession.Socket.Uri}");
            OnChannelClosing?.Invoke(session);
            bot.ActiveSessions.Remove(channelId);
            if (session.ArchipelagoSession.Socket.Connected) { await session.ArchipelagoSession.Socket.DisconnectAsync(); }

            foreach (var A in session.GetAuxiliarySlotNames())
            {
                var auxSession = session.GetAuxiliarySession(A)!;
                if (auxSession.Socket.Connected) { await auxSession.Socket.DisconnectAsync(); }
            }

            OnChannelClosed?.Invoke(session);
        }

        /// <summary>
        /// Sets up message handlers for the primary Archipelago session connection.
        /// </summary>
        /// <param name="botSession">The active bot session.</param>
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession)
        {
            botSession.ArchipelagoSession.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;
            botSession.ArchipelagoSession.Socket.SocketClosed += async (reason) =>
            {
                // Note: This event is not reliably triggered when the AP server closes,
                // as it primarily fires when the bot disconnects manually.
                // Server closures are currently handled in `CheckServerConnection`.
                if (!botSession.ParentBot.ActiveSessions.ContainsKey(botSession.DiscordChannel.Id)) { return; } //Bot was disconnected already
                await CleanAndCloseChannel(botSession.ParentBot, botSession.DiscordChannel.Id);
                var DisconnectEmbed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle("Session Disconnected")
                    .WithFields(
                        new EmbedFieldBuilder().WithName("Server").WithValue(botSession.ConnectionInfo.ToFormattedJson()),
                        new EmbedFieldBuilder().WithName("Reason").WithValue("Archipelago server closed")
                    ).Build();
                botSession.ParentBot.QueueAPIAction(botSession.DiscordChannel, new MessageQueueData.QueuedMessage(DisconnectEmbed));
            };
            void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) { return; }

                botSession.QueueMessageForChannel(message.FormatLogMessage(botSession));
            }
        }

        /// <summary>
        /// Monitors active Archipelago sessions and closes any that are disconnected.
        /// </summary>
        /// <param name="discordBot">The Discord bot instance managing the sessions.</param>
        public static async Task MonitorAndHandleAPServerClose(this DiscordBot discordBot)
        {
            while (true)
            {
                ulong[] CurrentSessions = [.. discordBot.ActiveSessions.Keys];
                foreach (var i in CurrentSessions)
                {
                    if (!discordBot.ActiveSessions.TryGetValue(i, out var session)) continue;
                    if (!session.ArchipelagoSession.Socket.Connected)
                    {
                        await discordBot.CleanAndCloseChannel(i);
                        var DisconnectEmbed = new EmbedBuilder()
                            .WithColor(Color.Orange)
                            .WithTitle("Session Disconnected")
                            .WithFields(
                                new EmbedFieldBuilder().WithName("Server").WithValue(session.ConnectionInfo.ToFormattedJson()),
                                new EmbedFieldBuilder().WithName("Reason").WithValue("Archipelago server closed")
                            ).Build();
                        discordBot.QueueAPIAction(session.DiscordChannel, new MessageQueueData.QueuedMessage(DisconnectEmbed));
                    }
                }
                await Task.Delay(500);
            }
        }

        /// <summary>
        /// Disconnects all active Archipelago clients and clears the bot's session list.
        /// </summary>
        /// <param name="discordBot">The Discord bot instance managing the sessions.</param>
        public static async Task DisconnectAllClients(this DiscordBot discordBot)
        {
            Console.WriteLine("Disconnecting all clients...");
            foreach (var session in discordBot.ActiveSessions.Values)
            {
                Console.WriteLine(session.DiscordChannel.Name);
                await CleanAndCloseChannel(discordBot, session.DiscordChannel.Id);
                var DisconnectEmbed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle("Session Disconnected")
                    .WithFields(
                        new EmbedFieldBuilder().WithName("Server").WithValue(session.ConnectionInfo.ToFormattedJson()),
                        new EmbedFieldBuilder().WithName("Reason").WithValue("Bot has exited")
                    ).Build();
                discordBot.QueueAPIAction(session.DiscordChannel, new MessageQueueData.QueuedMessage(DisconnectEmbed));
            }
            Console.WriteLine("Waiting for Queue to clear...");
            while (discordBot.DiscordAPIQueue.Queue.Count > 0)
            {
                await Task.Delay(20);
            }
            discordBot.DiscordAPIQueue.IsProcessing = false;
            await Task.Delay(2000);
        }

        public static bool ConnectToAPServer(
            DiscordBot discordBot, 
            ISocketMessageChannel channel, 
            Sessions.SessionConstructor sessionConstructor,
            out string Message)
        {
            try
            {
                var session = ArchipelagoSessionFactory.CreateSession(sessionConstructor.ArchipelagoConnectionInfo!.IP, sessionConstructor.ArchipelagoConnectionInfo!.Port);

                LoginResult result = session.TryConnectAndLogin(
                    null, //Game is not needed since we connect with the TextOnly Tag
                    sessionConstructor.ArchipelagoConnectionInfo!.Name,
                    ItemsHandlingFlags.AllItems,
                    Constants.APVersion,
                    [Constants.ArchipelagoTags.TextOnly.ToString()], null,
                    sessionConstructor.ArchipelagoConnectionInfo!.Password,
                    true);

                if (result is LoginFailure failure)
                {
                    var errors = string.Join("\n", failure.Errors);
                    Message =
                        $"Failed to connect to Archipelago server at " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                        $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}.\n" +
                        $"{errors}";
                    return false;
                }

                var NewSession = new Sessions.ActiveBotSession(sessionConstructor, discordBot, channel, session);
                discordBot.ActiveSessions[channel.Id] = NewSession;

                discordBot.UpdateConnectionCache(channel.Id, sessionConstructor);

                NewSession.CreateArchipelagoHandlers();
                _ = NewSession.MessageQueue.ProcessChannelMessages();

                OnSessionCreated?.Invoke(NewSession);

                Message = $"Successfully connected channel {channel.Name} to Archipelago server at " +
                    $"{NewSession.ArchipelagoSession.Socket.Uri.Host}:" +
                    $"{NewSession.ArchipelagoSession.Socket.Uri.Port} as " +
                    $"{NewSession.ArchipelagoSession.Players.ActivePlayer.Name} playing " +
                    $"{NewSession.ArchipelagoSession.Players.ActivePlayer.Game}.";
                return true;
            }
            catch (Exception ex)
            {
                Message = $"Failed to connect to Archipelago server at " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.IP}:" +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Port} as " +
                    $"{sessionConstructor.ArchipelagoConnectionInfo!.Name}.\n" +
                    $"{ex}";
                return false;
            }
        }

        public static void ConnectAuxiliarySessions(this Sessions.ActiveBotSession session, HashSet<string> Slots, out HashSet<string> FailedLogins, out HashSet<string> CreatedSessions)
        {
            FailedLogins = [];
            CreatedSessions = [];
            foreach (var slot in Slots)
            {
                var AuxConnection = session.GetAuxiliarySession(slot);
                if (AuxConnection is null)
                {
                    FailedLogins.Add(slot);
                    continue;
                }

                var ConnectionResult = AuxConnection.TryConnectAndLogin(
                    null,
                    slot,
                    ItemsHandlingFlags.AllItems,
                    Constants.APVersion,
                    [Constants.ArchipelagoTags.TextOnly.ToString()],
                    null,
                    session.ConnectionInfo.Password);

                if (ConnectionResult is LoginSuccessful)
                    CreatedSessions.Add(slot);
                else
                    FailedLogins.Add(slot);
            }

            OnAuxSessionCreated?.Invoke(session, CreatedSessions);
        }

        public static void DisconnectAuxiliarySessions(this Sessions.ActiveBotSession session, HashSet<string> Slots, out HashSet<string> FailedLogouts, out HashSet<string> RemovedSessions)
        {
            //Results
            FailedLogouts = [];
            RemovedSessions = [];
            foreach (var slot in Slots)
            {
                var AuxConnection = session.GetAuxiliarySession(slot);
                if (AuxConnection is null)
                {
                    FailedLogouts.Add(slot);
                    continue;
                }
                try
                {
                    AuxConnection!.Socket.DisconnectAsync();
                    RemovedSessions.Add(slot);
                }
                catch
                {
                    FailedLogouts.Add(slot);
                }
            }

            OnAuxSessionClosed?.Invoke(session, RemovedSessions);
        }
    }
}
