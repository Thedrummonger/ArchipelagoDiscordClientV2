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
        /// Cleans up and closes an active Archipelago session associated with a specific Discord channel.
        /// </summary>
        /// <param name="bot">The Discord bot managing the session.</param>
        /// <param name="channelId">The Discord channel ID linked to the session.</param>
        public static async Task CleanAndCloseChannel(this DiscordBot bot, ulong channelId)
        {
            if (!bot.ActiveSessions.TryGetValue(channelId, out var session)) { return; }
            bot.ActiveSessions.Remove(channelId);
            Console.WriteLine($"Disconnecting Channel {session.DiscordChannel.Name} from server {session.ArchipelagoSession.Socket.Uri}");
            if (session.ArchipelagoSession.Socket.Connected) { await session.ArchipelagoSession.Socket.DisconnectAsync(); }
            if (session.AuxiliarySessions.Count > 0)
            {
                foreach (var auxSession in session.AuxiliarySessions.Values)
                {
                    if (auxSession.Socket.Connected) { await auxSession.Socket.DisconnectAsync(); }
                }
                session.AuxiliarySessions.Clear();
            }
        }
        /// <summary>
        /// Sets up message handlers for an auxiliary Archipelago session.
        /// </summary>
        /// <param name="botSession">The active bot session.</param>
        /// <param name="auxiliaryConnection">The auxiliary Archipelago session.</param>
        public static void CreateArchipelagoHandlers(this Sessions.ActiveBotSession botSession, ArchipelagoSession auxiliaryConnection)
        {
            auxiliaryConnection.MessageLog.OnMessageReceived += MessageLog_OnMessageReceived;

            void MessageLog_OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
            {
                if (ArchipelagoMessageHelper.ShouldIgnoreMessage(message, botSession)) return;
                switch (message)
                {
                    case HintItemSendLogMessage hintItemSendLogMessage:
                        var queuedMessage = new MessageQueueData.QueuedItemLogMessage(message.ToColoredString(), message.ToString(), message.GetUserPings(botSession));
                        botSession.QueueMessageForChannel(queuedMessage);
                        break;
                    case CommandResultLogMessage commandResultLogMessage:
                        var Message = new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).Build());
                        botSession.QueueMessageForChannel(Message);
                        break;
                }
            }
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

                MessageQueueData.IQueuedMessage queuedMessage = message switch
                {
                    ItemSendLogMessage => new MessageQueueData.QueuedItemLogMessage(message.ToColoredString(), message.ToString(), message.GetUserPings(botSession)),
                    JoinLogMessage => new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).WithColor(Color.Green).Build()),
                    LeaveLogMessage => new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).WithColor(Color.Red).Build()),
                    CollectLogMessage => new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).WithColor(Color.Blue).Build()),
                    ReleaseLogMessage => new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).WithColor(Color.Blue).Build()),
                    GoalLogMessage => new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).WithColor(Color.Gold).Build()),
                    ChatLogMessage or ServerChatLogMessage => new MessageQueueData.QueuedMessage(message.ToString()),
                    _ => new MessageQueueData.QueuedMessage(new EmbedBuilder().WithDescription(message.ToString()).Build()),
                };
                botSession.QueueMessageForChannel(queuedMessage);
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
                    ["TextOnly"], null,
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

        public static void ConnectAuxiliarySessions(this Sessions.ActiveBotSession session, HashSet<PlayerInfo> Slots, out HashSet<string> FailedLogins, out HashSet<string> CreatedSessions)
        {
            FailedLogins = [];
            CreatedSessions = [];
            foreach (var slot in Slots)
            {
                var supportSession = ArchipelagoSessionFactory.CreateSession(session.ArchipelagoSession.Socket.Uri);
                var ConnectionResult = supportSession.TryConnectAndLogin(
                    slot.Game,
                    slot.Name,
                    ItemsHandlingFlags.AllItems,
                    Constants.APVersion,
                    ["TextOnly"],
                    null,
                    session.ConnectionInfo.Password);
                if (ConnectionResult is LoginSuccessful)
                {
                    CreatedSessions.Add(slot.Name);
                    session.AuxiliarySessions.Add(slot.Name, supportSession);
                    session.CreateArchipelagoHandlers(supportSession);
                }
                else
                {
                    FailedLogins.Add(slot.Name);
                }
            }
        }
    }
}
