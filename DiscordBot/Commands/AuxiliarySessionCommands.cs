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

                var TargetSlots = string.IsNullOrWhiteSpace(SlotArgs) ? session.GetAuxiliarySlotNames(false) : SlotArgs.TrimSplit(",");

                HashSet<string> ConnectedSlots = [..TargetSlots.Where(x => session.GetAuxiliarySlotNames(true).Contains(x))];
                HashSet<string> InvalidSlotNames = [.. TargetSlots.Where(x => !session.GetAuxiliarySlotNames().Contains(x))];
                HashSet<string> DisconnectedSlots = [.. TargetSlots.Where(x => session.GetAuxiliarySlotNames(false).Contains(x))];

                await command.RespondAsync(embed: DisconnectedSlots.Select(x => x).CreateEmbedResultsList("Attempting to add auxiliary connections for"));

                session.ConnectAuxiliarySessions(DisconnectedSlots, out HashSet<string> FailedLogins, out HashSet<string> CreatedSessions);

                var Result = CommandHelpers.CreateCommandResultEmbed("Add Auxiliary Sessions Results",
                    null,
                    ColorHelpers.GetResultEmbedStatusColor(CreatedSessions.Count, FailedLogins.Count + ConnectedSlots.Count + InvalidSlotNames.Count),
                    ("Sessions Created", CreatedSessions),
                    ("Failed Logins", FailedLogins),
                    ("Already Connected", ConnectedSlots),
                    ("Invalid Slot Name", InvalidSlotNames));

                await command.ModifyOriginalResponseAsync(x => x.Embed = Result.Build());
            }

            async Task Remove(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                var TargetSlots = string.IsNullOrWhiteSpace(SlotArgs) ? session.GetAuxiliarySlotNames(true) : SlotArgs.TrimSplit(",");

                HashSet<string> ConnectedSlots = [.. TargetSlots.Where(x => session.GetAuxiliarySlotNames(true).Contains(x))];
                HashSet<string> InvalidSlotNames = [.. TargetSlots.Where(x => !session.GetAuxiliarySlotNames().Contains(x))];
                HashSet<string> DisconnectedSlots = [.. TargetSlots.Where(x => session.GetAuxiliarySlotNames(false).Contains(x))];

                await command.RespondAsync(embed: ConnectedSlots.CreateEmbedResultsList("Attempting to disconnect auxiliary connections for"));

                //Results
                session.DisconnectAuxiliarySessions(ConnectedSlots, out HashSet<string> FailedLogouts, out HashSet<string> RemovedSessions);

                var Result = CommandHelpers.CreateCommandResultEmbed("Remove Auxiliary Sessions Results",
                    null,
                    ColorHelpers.GetResultEmbedStatusColor(RemovedSessions.Count, FailedLogouts.Count + DisconnectedSlots.Count + InvalidSlotNames.Count),
                    ("Sessions Removed", RemovedSessions),
                    ("Failed Logouts", FailedLogouts),
                    ("Not Connected", DisconnectedSlots),
                    ("Invalid Slot Name", InvalidSlotNames));

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
                var AuxiliarySession = session!.GetAuxiliarySession(SlotArgs);
                if (SlotArgs == session!.ArchipelagoSession.Players.ActivePlayer.Name)
                    TargetSession = session.ArchipelagoSession;
                else if (AuxiliarySession is not null && AuxiliarySession.Socket.Connected)
                    TargetSession = AuxiliarySession;
                else
                {
                    await command.RespondAsync("The given slot did not have an active connection", ephemeral: true);
                    return;
                }
                await command.RespondAsync($"[{TargetSession.Players.ActivePlayer.Name}] {MessageArgs}", ephemeral: true);
                TargetSession.Say(MessageArgs);
            }
        }
    }
}
