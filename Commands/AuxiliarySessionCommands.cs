using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace ArchipelagoDiscordClientLegacy.Commands
{
    internal class AuxiliarySessionCommands
    {
        public class AddAuxiliarySessionsCommand : ICommand
        {
            public string Name => "add_auxiliary_sessions";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Creates auxiliary connections to the given slots to allow interaction with those slots")
                    .AddOption("slots", ApplicationCommandOptionType.String, "Slots to create a auxiliary connection", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string Result))
                {
                    await command.RespondAsync(Result, ephemeral: true);
                    return;
                }
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                //Results
                HashSet<string> CreatedSessions = [];
                HashSet<string> FailedLogins = [];
                HashSet<string> AlreadyConnectedSlots = [];
                HashSet<string> InvalidSlotNames = [];

                HashSet<PlayerInfo> AllOtherPlayer = session!.archipelagoSession.Players.AllPlayers
                    .Where(x => x.Slot != session.archipelagoSession.Players.ActivePlayer.Slot && x.Name != "Server").ToHashSet();
                HashSet<PlayerInfo> ValidSlots = [];

                if (String.IsNullOrWhiteSpace(SlotArgs))
                {
                    ValidSlots = AllOtherPlayer.Where(x => !session.SupportSessions.ContainsKey(x.Name)).ToHashSet();
                }
                else
                {
                    var SlotArgsList = SlotArgs.TrimSplit(",");
                    foreach (var arg in SlotArgsList)
                    {
                        var playerInfo = AllOtherPlayer.FirstOrDefault(x => x.Name == arg);
                        if (playerInfo is null) InvalidSlotNames.Add(arg);
                        else if (session.SupportSessions.ContainsKey(arg)) AlreadyConnectedSlots.Add(arg);
                        else ValidSlots.Add(playerInfo);
                    }
                }

                await command.RespondAsync($"Attempting to add auxiliary connections for\n{string.Join(", ", ValidSlots.Select(x => x.Name))}.");
                foreach (var slot in ValidSlots)
                {
                    var supportSession = ArchipelagoSessionFactory.CreateSession(session.archipelagoSession.Socket.Uri);
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
                        session.SupportSessions.Add(slot.Name, supportSession);
                        session.CreateArchipelagoHandlers(discordBot, supportSession);
                    }
                    else
                    {
                        FailedLogins.Add(slot.Name);
                    }
                }

                List<string> MessageParts =
                    [
                    ..CreatedSessions.CreateResultList($"The following sessions were created"),
                    ..FailedLogins.CreateResultList($"Failed to login to the following sessions"),
                    ..AlreadyConnectedSlots.CreateResultList($"The following slots already had auxiliary connection"),
                    ..InvalidSlotNames.CreateResultList($"The following slots were not valid in the connected AP server"),
                    ];
                await command.ModifyOriginalResponseAsync(x => x.Content = String.Join("\n", MessageParts));
            }
        }
        public class RemoveAuxiliarySessionsCommand : ICommand
        {
            public string Name => "remove_auxiliary_sessions";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Closes and Removes the given Auxiliary connections")
                    .AddOption("slots", ApplicationCommandOptionType.String, "Slots to close Auxiliary connection for", false).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string Result))
                {
                    await command.RespondAsync(Result, ephemeral: true);
                    return;
                }
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                HashSet<string> ActiveAuxSessions = [.. session!.SupportSessions.Keys];
                HashSet<string> SessionToRemove =
                    String.IsNullOrWhiteSpace(SlotArgs) ?
                    [.. session!.SupportSessions.Keys] :
                    [.. SlotArgs.TrimSplit(",")];

                await command.RespondAsync($"Attempting to disconnect auxiliary connections for\n{string.Join(", ", SessionToRemove.Select(x => x))}.");

                //Results
                HashSet<string> RemovedSessions = [];
                HashSet<string> NotConnectedSlots = [];
                foreach (var Session in SessionToRemove)
                {
                    var Valid = session!.SupportSessions.TryGetValue(Session, out ArchipelagoSession? APSession);
                    if (Valid)
                    {
                        session!.SupportSessions.Remove(Session);
                        await APSession!.Socket.DisconnectAsync();
                        RemovedSessions.Add(Session);
                    }
                    else
                    {
                        NotConnectedSlots.Add(Session);
                    }
                }

                List<string> MessageParts =
                    [
                    ..RemovedSessions.CreateResultList($"The following sessions were removed"),
                    ..NotConnectedSlots.CreateResultList($"The following slots did not have an auxiliary connection"),
                    ];
                await command.ModifyOriginalResponseAsync(x => x.Content = String.Join("\n", MessageParts));
            }
        }

        public class SendAsAuxiliarySession : ICommand
        {
            public string Name => "send_as";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Sends a message as the given auxiliary slot")
                .AddOption("slot", ApplicationCommandOptionType.String, "Slot to send as", true)
                .AddOption("message", ApplicationCommandOptionType.String, "Message to send", true).Build();

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string Result))
                {
                    await command.RespondAsync(Result, ephemeral: true);
                    return;
                }
                var SlotArgs = commandData.GetArg("slot")?.GetValue<string>();
                var MessageArgs = commandData.GetArg("message")?.GetValue<string>();
                if (String.IsNullOrWhiteSpace(MessageArgs) || String.IsNullOrWhiteSpace(SlotArgs))
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }
                ArchipelagoSession TargetSession;
                if (SlotArgs == session!.archipelagoSession.Players.ActivePlayer.Name)
                {
                    TargetSession = session.archipelagoSession;
                }
                else if (session!.SupportSessions.TryGetValue(SlotArgs, out ArchipelagoSession? AuxiliarySession))
                {
                    TargetSession = AuxiliarySession;
                }
                else
                {
                    await command.RespondAsync("The given slot did not have an active auxiliary connection", ephemeral: true);
                    return;
                }
                await command.RespondAsync($"[{TargetSession.Players.ActivePlayer.Name}] {MessageArgs}");
                TargetSession.Say(MessageArgs);
            }
        }
    }
}
