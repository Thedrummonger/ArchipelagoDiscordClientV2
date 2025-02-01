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

                discordBot.UpdateConnectionCache(commandData.ChannelId);

            }

            async Task Add(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                //Results
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
                if (ValidSlots.Count == 0)
                {
                    await command.RespondAsync($"No valid slots given");
                    return;
                }
                await command.RespondAsync(embed: ValidSlots.Select(x => x.Name).CreateEmbedResultsList("Attempting to add auxiliary connections for"));

                session.ConnectAuxiliarySessions(ValidSlots, out HashSet<string> FailedLogins, out var CreatedSessions);

                var Result = CommandHelpers.CreateCommandResultEmbed("Add Auxiliary Sessions Results", Color.Green,
                    ("Sessions Created", CreatedSessions),
                    ("Failed Logins", FailedLogins),
                    ("Already Connected", AlreadyConnectedSlots),
                    ("Invalid Slot Name", InvalidSlotNames));

                await command.ModifyOriginalResponseAsync(x => x.Embed = Result.Build());
            }

            async Task Remove(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                HashSet<string> ActiveAuxSessions = [.. session!.AuxiliarySessions.Keys];
                HashSet<string> SessionToRemove =
                    String.IsNullOrWhiteSpace(SlotArgs) ?
                    [.. session!.AuxiliarySessions.Keys] :
                    [.. SlotArgs.TrimSplit(",")];

                await command.RespondAsync(embed: SessionToRemove.CreateEmbedResultsList("Attempting to disconnect auxiliary connections for"));

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

                var Result = CommandHelpers.CreateCommandResultEmbed("Remove Auxiliary Sessions Results", Color.Green,
                    ("Removed Sessions", RemovedSessions),
                    ("Session not Found", NotConnectedSlots));

                await command.ModifyOriginalResponseAsync(x => x.Embed = Result.Build());
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
