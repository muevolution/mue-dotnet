using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Scripting;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.System;
using Mue.Server.Core.Utils;

namespace Mue.Server.Core.Scripting.Implementation
{
    public class MueScriptWorld
    {
        private IWorld _world;
        private MueEngineExecutor _executor;

        public MueScriptWorld(IWorld world, MueEngineExecutor executor)
        {
            _world = world;
            _executor = executor;
        }

        [MueExposedScriptMethod]
        public Task TellShort(string message, string target = null, IDictionary<string, string> meta = null)
        {
            return TellAsync(message, target, meta);
        }

        [MueExposedScriptMethod]
        public Task TellExtended(IDictionary<string, string> extendedFormat, IDictionary<string, string> extendedContent, string target = null, IDictionary<string, string> meta = null)
        {
            var msgFormats = new MessageFormats
            {
                FirstPerson = extendedFormat["FirstPerson"],
                ThirdPerson = extendedFormat["ThirdPerson"],
            };

            return TellAsync(null, target, meta, msgFormats, extendedContent);
        }

        [MueExposedScriptMethod]
        public Task TellTable(IEnumerable<IEnumerable<string>> table, string message = null, bool hasHeader = false, string target = null, IDictionary<string, string> meta = null)
        {
            var fullMeta = meta ?? new Dictionary<string, string>();
            fullMeta[CommunicationsMessage.META_RENDERER] = CommunicationsMessage.META_RENDERER_TABLE;
            fullMeta[CommunicationsMessage.META_TABLE_CONTENT] = Json.Serialize(new CommunicationsMessage_Table
            {
                Message = message,
                Table = table,
                HasHeader = hasHeader,
            });

            return TellAsync(CommunicationsMessage.MSG_NO_TABLES, target, fullMeta);
        }

        private async Task TellAsync(string message, string target = null, IDictionary<string, string> meta = null, MessageFormats? extendedFormat = null, IDictionary<string, string> extendedContent = null)
        {
            var targetObjId = new ObjectId(target != null ? target : _executor.RunBy);
            var targetObj = await _world.GetObjectById(targetObjId);
            if (targetObj == null)
            {
                throw new MueEngineBindingException($"Target {target ?? "null!"} not found");
            }

            if (meta != null)
            {
                CommunicationsMessage.PurifyMeta(meta);
            }

            await _world.PublishMessage(new InteriorMessage
            {
                Message = message,
                Meta = meta,
                ExtendedContent = extendedContent != null ? new ReadOnlyDictionary<string, string>(extendedContent) : null,
                ExtendedFormat = extendedFormat,
                Source = _executor.RunBy,
                Script = _executor.ThisScript,
            }, targetObj);
        }

        [MueExposedScriptMethod]
        public async Task<IEnumerable<string>> GetConnectedPlayers()
        {
            var players = await _world.GetConnectedPlayerIds();
            return players.Select(s => s.Id);
        }

        [MueExposedScriptMethod]
        public async Task<string> GetPlayerIdFromName(string playerName)
        {
            var player = await _world.GetPlayerByName(playerName);
            return player?.Id.Id;
        }

        [MueExposedScriptMethod]
        public async Task<string> GetName(string objectId)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            return obj?.Name;
        }

        [MueExposedScriptMethod]
        public async Task<string> GetParent(string objectId)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            return obj?.Parent?.Id;
        }

        [MueExposedScriptMethod]
        public async Task<string> GetLocation(string objectId)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            return obj?.Location?.Id;
        }

        [MueExposedScriptMethod]
        public async Task<string> Find(string target)
        {
            var runBy = await _world.GetObjectById<GamePlayer>(new ObjectId(_executor.RunBy));
            var obj = await runBy.ResolveTarget(target);
            return obj?.Id;
        }

        [MueExposedScriptMethod]
        public async Task<DynamicDictionary> GetDetails(string objectId)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            if (obj == null)
            {
                return null;
            }

            var meta = obj.MetaBasic.ToDictionary();
            dynamic output = DynamicDictionary.From(meta);
            output.type = obj.Id.ObjectType.ToShortString();
            ((DynamicDictionary)output).Lock();
            return output;
        }

        [MueExposedScriptMethod]
        public async Task<dynamic> GetProp(string objectId, string path)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            if (obj == null)
            {
                return null;
            }
            var val = await obj.GetProp(path);
            return val.ToDynamic();
        }

        [MueExposedScriptMethod]
        public async Task<IDictionary<string, dynamic>> GetProps(string objectId)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            if (obj == null)
            {
                return null;
            }
            var val = await obj.GetProps();
            return val.ToDictionary(s => s.Key, s => s.Value.ToDynamic());
        }

        [MueExposedScriptMethod]
        public async Task<IEnumerable<string>> GetContents(string objectId, string type = null)
        {
            var obj = await _world.GetObjectById(new ObjectId(objectId));
            if (obj == null || obj is not IContainer)
            {
                return null;
            }

            GameObjectType? dType = type != null ? GameObjectConsts.GetGameObjectType(type) : null;

            var val = await (obj as IContainer).GetContents(dType);
            return val.Select(s => s.Id);
        }
    }
}