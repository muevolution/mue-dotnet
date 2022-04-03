using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.System.CommandBuiltins
{
    public partial class BuiltinCommands
    {
        private const string WatchFor_ListPropPath = "_/watchfor/list";
        private const string WatchFor_Messages_NobodyOnline = "Nobody you are watching for is online.";
        private const string WatchFor_Messages_NoWatches = "You are not watching for anyone.";
        private Dictionary<string, string> WatchFor_DefaultMeta = new Dictionary<string, string> {
            {CommunicationsMessage.META_ORIGIN, "watchfor"},
        };

        [BuiltinCommand("wf")]
        [BuiltinCommand("watchfor")]
        public async Task WatchFor(GamePlayer player, LocalCommand command)
        {
            var isChange = false;
            var isAdding = true;
            string targetName = null;

            if (command.Params != null)
            {
                return;
            }
            else if (!String.IsNullOrWhiteSpace(command.Args))
            {
                var subcmd = command.Args.Split(" ", 1).FirstOrDefault();
                if (subcmd.StartsWith("#"))
                {
                    await (subcmd switch
                    {
                        "#help" => WatchFor_Help(player),
                        "#list" => WatchFor_List(player),
                        _ => _world.PublishMessage("I don't understand that command.", player, WatchFor_DefaultMeta),
                    });
                    return;
                }
                else if (subcmd.StartsWith("!"))
                {
                    isChange = true;
                    isAdding = false;
                    targetName = subcmd.Substring(1);
                }
                else
                {
                    isChange = true;
                    isAdding = true;
                    targetName = subcmd;
                }
            }

            if (isChange && targetName != null)
            {
                await WatchFor_AddRemove(player, targetName, !isAdding);
                return;
            }

            // Bare operation
            await WatchFor_Online(player);
        }

        private async Task WatchFor_Help(GamePlayer player)
        {
            await _world.PublishMessage("Help goes here", player, WatchFor_DefaultMeta);
        }

        private async Task WatchFor_List(GamePlayer player)
        {
            var propMgr = new ObjectIdPropListHelper(player, WatchFor_ListPropPath);
            var interestedPlayerIds = await propMgr.All();
            var interestedPlayers = (await Task.WhenAll(interestedPlayerIds.Select(s => _world.GetObjectById<GamePlayer>(s)))).WhereNotNull().ToList();

            if (interestedPlayers.Count > 0)
            {
                var playerNames = interestedPlayers.Select(s => s.Name);

                var msg = "Players you are watching for: " + String.Join(", ", playerNames);
                await _world.PublishMessage(msg, player, new Dictionary<string, string>(WatchFor_DefaultMeta) {
                    {CommunicationsMessage.META_RENDERER, CommunicationsMessage.META_RENDERER_LIST},
                    {CommunicationsMessage.META_LIST_CONTENT, Json.Serialize(new CommunicationsMessage_List {
                        Message = "Players you are watching for:",
                        List = playerNames
                    })},
                    {"watchfor_list", Json.Serialize(interestedPlayers.Select(s => new {Id = s.Id, Name = s.Name}))}
                });
            }
            else
            {
                await _world.PublishMessage(WatchFor_Messages_NoWatches, player, WatchFor_DefaultMeta);
            }
        }

        private async Task WatchFor_AddRemove(GamePlayer player, string target, bool isRemoving)
        {
            var targetPlayer = await _world.GetPlayerByName(target);
            if (targetPlayer == null)
            {
                await _world.PublishMessage(MSG_NOTFOUND_PLAYER, player, WatchFor_DefaultMeta);
                return;
            }

            if (targetPlayer.Id == player.Id)
            {
                await _world.PublishMessage("You cannot watch for yourself!", player, WatchFor_DefaultMeta);
                return;
            }

            var propMgr = new ObjectIdPropListHelper(player, WatchFor_ListPropPath);

            string msg;
            if (!isRemoving)
            {
                if (await propMgr.Add(targetPlayer.Id))
                {
                    msg = $"{targetPlayer.Name} was added to your watchlist.";
                }
                else
                {
                    msg = $"{targetPlayer.Name} was already in your watchlist.";
                }
            }
            else
            {
                if (await propMgr.Remove(targetPlayer.Id))
                {
                    msg = $"{targetPlayer.Name} was removed from your watchlist.";
                }
                else
                {
                    msg = $"{targetPlayer.Name} was not in your watchlist.";
                }
            }

            await _world.PublishMessage(msg, player, WatchFor_DefaultMeta);
        }

        private async Task WatchFor_Online(GamePlayer player)
        {
            var propMgr = new ObjectIdPropListHelper(player, WatchFor_ListPropPath);
            var interestedPlayerIds = await propMgr.All();
            if (interestedPlayerIds.Count < 1)
            {
                await _world.PublishMessage(WatchFor_Messages_NoWatches, player, WatchFor_DefaultMeta);
                return;
            }

            var onlinePlayers = await _world.GetConnectedPlayerIds();
            var interestedOnlinePlayerIds = onlinePlayers.Where(w => interestedPlayerIds.Contains(w));
            var interestedOnlinePlayers = (await Task.WhenAll(interestedOnlinePlayerIds.Select(s => _world.GetObjectById<GamePlayer>(s)))).WhereNotNull().ToList();

            if (interestedOnlinePlayers.Count > 0)
            {
                var playerNames = interestedOnlinePlayers.Select(s => s.Name);

                var msg = "The players you are watching for are online: " + String.Join(", ", playerNames);
                await _world.PublishMessage(msg, player, new Dictionary<string, string>(WatchFor_DefaultMeta) {
                    {CommunicationsMessage.META_RENDERER, CommunicationsMessage.META_RENDERER_LIST},
                    {CommunicationsMessage.META_LIST_CONTENT, Json.Serialize(new CommunicationsMessage_List {
                        Message = "The players you are watching for are online:",
                        List = playerNames
                    })},
                    {"watchfor_list", Json.Serialize(interestedOnlinePlayers.Select(s => new {Id = s.Id, Name = s.Name}))}
                });
            }
            else
            {
                await _world.PublishMessage(WatchFor_Messages_NobodyOnline, player, WatchFor_DefaultMeta);
            }
        }

        [BuiltinSubscriber(typeof(PlayerUpdate), PlayerUpdate.EVENT_CONNECT, PlayerUpdate.EVENT_DISCONNECT)]
        public async Task<Unit> WatchFor_PlayerEventConsumer(ObjectUpdate update)
        {
            var updateePlayer = await _world.GetObjectById<GamePlayer>(update.Id);
            if (updateePlayer == null)
            {
                return Unit.Default;
            }

            // This seems like a bad way to do this but what do I know
            // TODO: Delay this by a bit?
            // Ask everyone online if they care about the person connecting/disconnecting
            var connectedPlayers = await _world.GetConnectedPlayerIds();
            await Task.WhenAll(
                connectedPlayers.AsParallel()
                    .Select(s => new { id = s, plh = new ObjectIdPropListHelper(_world, s, WatchFor_ListPropPath) })
                    .Select(async s => new { id = s.id, watching = await s.plh.Contains(update.Id) })
                    .Select(async s => (await s).watching ? WatchFor_NotifyPlayerOfStateChange((await s).id, updateePlayer, update.EventName, update.EventTime) : Task.CompletedTask)
            );
            return Unit.Default;
        }

        private async Task WatchFor_NotifyPlayerOfStateChange(ObjectId targetPlayerId, GamePlayer changePlayer, string newState, DateTime when)
        {
            // Future idea: Come up with some script registration system where a user can opt in to let a script handle this
            //   instead of being forced to use this (and maybe we can just use a default script for this part too)
            // Scripts could be triggered automatically in the user's context with some args (or maybe pull from the binding)
            // The system itself should handle the actual command, but this way the user could customize their responses without
            //   us having to add customization options

            var messageText = newState switch
            {
                PlayerUpdate.EVENT_CONNECT => "has connected",
                PlayerUpdate.EVENT_DISCONNECT => "has disconnected",
                _ => null,
            };
            if (messageText == null)
            {
                return;
            }

            var targetPlayer = await _world.GetObjectById<GamePlayer>(targetPlayerId);
            if (targetPlayer == null)
            {
                return;
            }

            if (targetPlayer.Location == changePlayer.Location)
            {
                // Hide if they're in the same room to prevent redundant messages
                return;
            }
            
            // TODO: We should send an unrendered client message if their state isn't changing (like a 2nd connection)

            await _world.PublishMessage(
                $" >>> {changePlayer.Name} {messageText} ({when.ToShortTimeString()}) <<<",
                targetPlayer,
                new Dictionary<string, string>(WatchFor_DefaultMeta) {
                    {"watchfor_change", Json.Serialize(new {Id = changePlayer.Id, Name = changePlayer.Name})}
                }
            );
        }
    }
}