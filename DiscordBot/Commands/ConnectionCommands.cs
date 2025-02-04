using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using static ArchipelagoDiscordClientLegacy.Data.DiscordBotData;
using static ArchipelagoDiscordClientLegacy.Data.MessageQueueData;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    public static class ConnectionCommands
    {
        public class ConnectCommand : ICommand
        {
            public string Name => "connect";
            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Connect this channel to an Archipelago server")
                    .AddOption("address", ApplicationCommandOptionType.String, "Server address. formatted {ip}:{port}", true)
                    .AddOption("name", ApplicationCommandOptionType.String, "Slot Name of the connecting player", true)
                    .AddOption("password", ApplicationCommandOptionType.String, "Server password (optional)", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, false, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(result, ephemeral: true);
                    return;
                }
                var IPArg = Data.GetArg("address")?.GetValue<string>();
                var NameArg = Data.GetArg("name")?.GetValue<string>();
                var PasswordArg = Data.GetArg("password")?.GetValue<string>();
                var (Ip, Port) = NetworkHelpers.ParseIpAddress(IPArg);
                if (Ip is null || string.IsNullOrWhiteSpace(NameArg))
                {
                    await command.RespondAsync("Connection args invalid.", ephemeral: true);
                    return;
                }
                var ConnectionData = new Sessions.ArchipelagoConnectionInfo
                {
                    IP = Ip,
                    Port = Port,
                    Name = NameArg,
                    Password = PasswordArg
                };

                var SessionInfo = new Sessions.SessionConstructor
                {
                    ArchipelagoConnectionInfo = ConnectionData,
                    Settings = discordBot.appSettings.AppDefaultSettings
                };

                await command.RespondAsync(embed: new EmbedBuilder().WithDescription($"Connecting {Data.ChannelName} to " +
                $"{SessionInfo.ArchipelagoConnectionInfo!.IP}:" +
                    $"{SessionInfo.ArchipelagoConnectionInfo!.Port} as " +
                    $"{SessionInfo.ArchipelagoConnectionInfo!.Name}").Build());

                var connectionResult = ArchipelagoConnectionHelpers.ConnectToAPServer(discordBot, Data.TextChannel!, SessionInfo, out var resultMessage);

                await command.ModifyOriginalResponseAsync(msg => msg.Embed = new EmbedBuilder()
                    .WithTitle(connectionResult ? "Connection Successful" : "Connection Failure")
                    .WithDescription(resultMessage)
                    .WithColor(connectionResult ? Color.Green : Color.Red)
                    .WithCurrentTimestamp()
                    .Build());
            }
        }

        public class ReConnectCommand : ICommand
        {
            public string Name => "reconnect";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .AddOption("auxiliary", ApplicationCommandOptionType.Boolean, "Reconnect Auxiliary Session?", false)
                .WithDescription("Connects to the server using the last known working connection info.").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, false, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription(result).WithColor(Color.Red).Build(), ephemeral: true);
                    return;
                }

                if (!discordBot.ConnectionCache.TryGetValue(Data.TextChannel!.Id, out SessionConstructor? SessionInfo) || SessionInfo is null)
                {
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription("No previous connection cached for this channel").WithColor(Color.Red).Build(), ephemeral: true);
                    return;
                }

                var ReconnectAuxiliaryArg = Data.GetArg("auxiliary")?.GetValue<bool>() ?? false;

                await command.RespondAsync(embed: new EmbedBuilder().WithDescription($"Connecting {Data.ChannelName} to " +
                $"{SessionInfo.ArchipelagoConnectionInfo!.IP}:" +
                    $"{SessionInfo.ArchipelagoConnectionInfo!.Port} as " +
                    $"{SessionInfo.ArchipelagoConnectionInfo!.Name}").Build());

                var connectionResult = ArchipelagoConnectionHelpers.ConnectToAPServer(discordBot, Data.TextChannel!, SessionInfo, out var resultMessage);

                var ResultEmbed = new EmbedBuilder()
                    .WithTitle(connectionResult ? "Connection Successful" : "Connection Failure")
                    .WithDescription(resultMessage)
                    .WithColor(connectionResult ? Color.Green : Color.Red)
                    .WithCurrentTimestamp();

                await command.ModifyOriginalResponseAsync(msg => msg.Embed = ResultEmbed.Build());
                if (!connectionResult) return;

                var Session = discordBot.ActiveSessions[Data.TextChannel!.Id];

                if (ReconnectAuxiliaryArg && SessionInfo.AuxiliarySessions.Count > 0)
                {
                    var Embeds = await ReconnectAuxiliary(command, SessionInfo, ResultEmbed, Session, discordBot);
                }
            }

            private static async Task<EmbedBuilder[]> ReconnectAuxiliary(
                SocketSlashCommand command, 
                SessionConstructor SessionInfo, 
                EmbedBuilder ResultEmbed, 
                ActiveBotSession Session, 
                DiscordBot discordBot)
            {
                EmbedBuilder AuxiliaryEmbed = new EmbedBuilder()
                    .WithDescription("Adding Auxiliary Sessions")
                    .AddField("Sessions", string.Join("\n", SessionInfo.AuxiliarySessions));
                EmbedBuilder[] SendEmbeds = [ResultEmbed, AuxiliaryEmbed];
                await command.ModifyOriginalResponseAsync(msg => msg.Embeds = SendEmbeds.Select(x => x.Build()).ToArray());
                HashSet<PlayerInfo> AuxiliarySlots = [];
                foreach (var Slot in SessionInfo.AuxiliarySessions)
                {
                    var slotInfo = Session.ArchipelagoSession.Players.AllPlayers.FirstOrDefault(x => x.Name == Slot);
                    if (slotInfo is null) continue;
                    AuxiliarySlots.Add(slotInfo);
                }
                Session.ConnectAuxiliarySessions(AuxiliarySlots, out var failedLogins, out var createdSessions);
                discordBot.UpdateConnectionCache(Session.DiscordChannel.Id);

                AuxiliaryEmbed = CommandHelpers.CreateCommandResultEmbed(
                    "Add Auxiliary Sessions Results",
                    null,
                    ColorHelpers.GetResultEmbedStatusColor(createdSessions, failedLogins),
                    ("Sessions Created", createdSessions),
                    ("Failed Logins", failedLogins));
                SendEmbeds = [ResultEmbed, AuxiliaryEmbed];
                await command.ModifyOriginalResponseAsync(msg => msg.Embeds = SendEmbeds.Select(x => x.Build()).ToArray());
                return SendEmbeds;
            }
        }

        public class DisconnectCommand : ICommand
        {
            public string Name => "disconnect";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Disconnect this channel from the Archipelago server").Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, true, out CommandData.CommandDataModel Data, out string result))
                {
                    await command.RespondAsync(embed: new EmbedBuilder().WithDescription(result).WithColor(Color.Red).Build(), ephemeral: true);
                    return;
                }
                await command.RespondAsync(embed: new EmbedBuilder().WithDescription("Disconnecting from the Archipelago server.").Build());

                await ArchipelagoConnectionHelpers.CleanAndCloseChannel(discordBot, Data.ChannelId);

                await command.ModifyOriginalResponseAsync(x => x.Embed = new EmbedBuilder()
                    .WithDescription("Successfully disconnected from the Archipelago server.")
                    .WithColor(Color.Orange).Build());
            }
        }
    }
}
