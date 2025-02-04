using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using ArchipelagoDiscordClientLegacy.Commands;
using ArchipelagoDiscordClientLegacy.Data;
using ArchipelagoDiscordClientLegacy.Helpers;
using Discord;
using Discord.WebSocket;
using TDMUtils;
using static ArchipelagoDiscordClientLegacy.Data.Sessions;

namespace DevClient.Commands
{
    internal class ArchipelagoItemClientCommands
    {
        public class ManageItemClientSessionsCommand : ICommand
        {
            public string Name => "edit_item_client_sessions";
            public SlashCommandProperties Properties => new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription("Adds or removes connection to the given slot that has item management permissions")
                .AddRemoveActionOption()
                .AddOption("slots", ApplicationCommandOptionType.String, "Slots to add or remove an item management connection", false).Build();

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
                if (action == (int)CommandHelpers.AddRemoveAction.add)
                {
                    await Add(command, discordBot, commandData, session!);
                }
                else
                {
                    await Remove(command, discordBot, commandData, session!);
                }
            }

            private async Task Add(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                //Results
                HashSet<string> AlreadyConnectedSlots = [];
                HashSet<string> InvalidSlotNames = [];

                HashSet<PlayerInfo> AllOtherPlayer = session!.ArchipelagoSession.Players.AllPlayers
                    .Where(x => x.Name != "Server").ToHashSet();
                HashSet<PlayerInfo> ValidSlots = [];

                if (!session.Metadata.TryGetValue(ItemManagementSessionManager.ManagerMetadataKey, out var v) || v is not ItemManagementSession itemManagementSession)
                {
                    await command.RespondAsync("Unhandled error, ItemManagementSession Metadata did not exist", ephemeral: true);
                    return;
                }

                if (String.IsNullOrWhiteSpace(SlotArgs))
                {
                    ValidSlots = AllOtherPlayer.Where(x => !itemManagementSession.ActiveItemClientSessions.ContainsKey(x.Name)).ToHashSet();
                }
                else
                {
                    var SlotArgsList = SlotArgs.TrimSplit(",");
                    foreach (var arg in SlotArgsList)
                    {
                        var playerInfo = AllOtherPlayer.FirstOrDefault(x => x.Name == arg);
                        if (playerInfo is null) InvalidSlotNames.Add(arg);
                        else if (itemManagementSession.ActiveItemClientSessions.ContainsKey(arg)) AlreadyConnectedSlots.Add(arg);
                        else ValidSlots.Add(playerInfo);
                    }
                }
                if (ValidSlots.Count == 0)
                {
                    await command.RespondAsync($"No valid slots given");
                    return;
                }
                await command.RespondAsync(embed: ValidSlots.Select(x => x.Name).CreateEmbedResultsList("Attempting to add Item Management Sessions for"));
                ConnectItemManagementSessions(session, itemManagementSession, ValidSlots, out HashSet<string> FailedLogins, out var CreatedSessions);

                var Result = CommandHelpers.CreateCommandResultEmbed("Add Item Management Sessions Results",
                    null,
                    ColorHelpers.GetResultEmbedStatusColor(CreatedSessions.Count, FailedLogins.Count + AlreadyConnectedSlots.Count + InvalidSlotNames.Count),
                    ("Sessions Created", CreatedSessions),
                    ("Failed Logins", FailedLogins),
                    ("Already Connected", AlreadyConnectedSlots),
                    ("Invalid Slot Name", InvalidSlotNames));

                await command.ModifyOriginalResponseAsync(x => x.Embed = Result.Build());
            }

            private async Task Remove(SocketSlashCommand command, DiscordBotData.DiscordBot discordBot, CommandData.CommandDataModel commandData, ActiveBotSession session)
            {
                var SlotArgs = commandData.GetArg("slots")?.GetValue<string>();

                if (!session.Metadata.TryGetValue(ItemManagementSessionManager.ManagerMetadataKey, out var v) || v is not ItemManagementSession itemManagementSession)
                {
                    await command.RespondAsync("Unhandled error, ItemManagementSession Metadata did not exist", ephemeral: true);
                    return;
                }

                HashSet<string> ActiveAuxSessions = [.. itemManagementSession!.ActiveItemClientSessions.Keys];
                HashSet<string> SessionToRemove =
                    String.IsNullOrWhiteSpace(SlotArgs) ?
                    [.. itemManagementSession!.ActiveItemClientSessions.Keys] :
                    [.. SlotArgs.TrimSplit(",")];

                await command.RespondAsync(embed: SessionToRemove.CreateEmbedResultsList("Attempting to disconnect Item Management Sessions for"));

                //Results
                HashSet<string> RemovedSessions = [];
                HashSet<string> NotConnectedSlots = [];
                foreach (var Session in SessionToRemove)
                {
                    var Valid = itemManagementSession!.ActiveItemClientSessions.TryGetValue(Session, out ArchipelagoSession? APSession);
                    if (Valid)
                    {
                        itemManagementSession!.ActiveItemClientSessions.Remove(Session);
                        await APSession!.Socket.DisconnectAsync();
                        RemovedSessions.Add(Session);
                    }
                    else
                    {
                        NotConnectedSlots.Add(Session);
                    }
                }

                var Result = CommandHelpers.CreateCommandResultEmbed("Remove Item Management Sessions Results", null,
                    ColorHelpers.GetResultEmbedStatusColor(RemovedSessions, NotConnectedSlots),
                    ("Removed Sessions", RemovedSessions),
                    ("Session not Found", NotConnectedSlots));

                await command.ModifyOriginalResponseAsync(x => x.Embed = Result.Build());
            }
        }

        public static void ConnectItemManagementSessions(ActiveBotSession session, ItemManagementSession itemManagementSession, HashSet<PlayerInfo> Slots, out HashSet<string> FailedLogins, out HashSet<string> CreatedSessions)
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
                    [],
                    null,
                    session.ConnectionInfo.Password);
                if (ConnectionResult is LoginSuccessful)
                {
                    CreatedSessions.Add(slot.Name);
                    itemManagementSession.ActiveItemClientSessions.Add(slot.Name, supportSession);
                }
                else
                {
                    FailedLogins.Add(slot.Name);
                }
            }
        }
    }
}
