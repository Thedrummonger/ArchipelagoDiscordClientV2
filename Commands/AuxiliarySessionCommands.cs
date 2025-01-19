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
            public string Name => "edit_auxiliary_sessions";

            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                    .WithDescription("Adds or removes auxiliary connections to the given slots")
                    .AddRemoveActionOption()
                    .AddOption("slots", ApplicationCommandOptionType.String, "Slots to add or remove a auxiliary connection", false).Build();
            public bool IsDebugCommand => false;

            public async Task ExecuteCommand(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot)
            {
                if (!command.Validate(discordBot, out ActiveBotSession? session, out CommandData.CommandDataModel commandData, out string Result))
                {
                    await command.RespondAsync(Result, ephemeral: true);
                    return;
                }
                var actionArg = commandData.GetArg(CommandHelpers.AddRemoveActionName)?.GetValue<long>();
                if (actionArg is not long action)
                {
                    await command.RespondAsync("Invalid arguments", ephemeral: true);
                    return;
                }
                else if (action == (int)CommandHelpers.AddRemoveAction.add)
                    await Add(command, discordBot, commandData, session!);
                else
                    await Remove(command, discordBot, commandData, session!);

            }

            async Task Add(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                //Results
                HashSet<string> CreatedSessions = [];
                HashSet<string> FailedLogins = [];
                HashSet<string> AlreadyConnectedSlots = [];
                HashSet<string> InvalidSlotNames = [];

                HashSet<PlayerInfo> AllOtherPlayer = session!.ArchipelagoSession.Players.AllPlayers
                    .Where(x => x.Slot != session.ArchipelagoSession.Players.ActivePlayer.Slot && x.Name != "Server").ToHashSet();
                HashSet<PlayerInfo> ValidSlots = [];

                if (String.IsNullOrWhiteSpace(SlotArgs))
                {
                    ValidSlots = AllOtherPlayer.Where(x => !session.AuxiliarySessions.ContainsKey(x.Name)).ToHashSet();
                }
                else
                {
                    var SlotArgsList = SlotArgs.TrimSplit(",");
                    foreach (var arg in SlotArgsList)
                    {
                        var playerInfo = AllOtherPlayer.FirstOrDefault(x => x.Name == arg);
                        if (playerInfo is null) InvalidSlotNames.Add(arg);
                        else if (session.AuxiliarySessions.ContainsKey(arg)) AlreadyConnectedSlots.Add(arg);
                        else ValidSlots.Add(playerInfo);
                    }
                }
                await command.RespondAsync(string.Join("\n", ValidSlots.Select(x => x.Name).CreateResultList("Attempting to add auxiliary connections for")));
                foreach (var slot in ValidSlots)
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

                List<string> MessageParts =
                    [
                    ..CreatedSessions.CreateResultList($"The following sessions were created"),
                    ..FailedLogins.CreateResultList($"Failed to login to the following sessions"),
                    ..AlreadyConnectedSlots.CreateResultList($"The following slots already had auxiliary connection"),
                    ..InvalidSlotNames.CreateResultList($"The following slots were not valid in the connected AP server"),
                    ];
                await command.ModifyOriginalResponseAsync(x => x.Content = String.Join("\n", MessageParts));
            }

            async Task Remove(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                HashSet<string> ActiveAuxSessions = [.. session!.AuxiliarySessions.Keys];
                HashSet<string> SessionToRemove =
                    String.IsNullOrWhiteSpace(SlotArgs) ?
                    [.. session!.AuxiliarySessions.Keys] :
                    [.. SlotArgs.TrimSplit(",")];

                await command.RespondAsync(string.Join("\n", SessionToRemove.CreateResultList("Attempting to disconnect auxiliary connections for")));

                //Results
                HashSet<string> RemovedSessions = [];
                HashSet<string> NotConnectedSlots = [];
                foreach (var Session in SessionToRemove)
                {
                    var Valid = session!.AuxiliarySessions.TryGetValue(Session, out ArchipelagoSession? APSession);
                    if (Valid)
                    {
                        session!.AuxiliarySessions.Remove(Session);
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
                .WithDescription("Sends a message as the given archipelago player")
                .AddOption("slot", ApplicationCommandOptionType.String, "Slot to send as", true)
                .AddOption("message", ApplicationCommandOptionType.String, "Message to send", true).Build();

            public bool IsDebugCommand => false;

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
                if (SlotArgs == session!.ArchipelagoSession.Players.ActivePlayer.Name)
                {
                    TargetSession = session.ArchipelagoSession;
                }
                else if (session!.AuxiliarySessions.TryGetValue(SlotArgs, out ArchipelagoSession? AuxiliarySession))
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
