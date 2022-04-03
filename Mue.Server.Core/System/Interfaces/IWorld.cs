using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Common.Models;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;
using Mue.Server.Core.Rx;

namespace Mue.Server.Core.System
{
    public interface IWorld
    {
        string WorldInstanceId { get; }

        Task Init();
        Task Shutdown();

        ICommandProcessor CommandProcessor { get; }
        IStorageManager StorageManager { get; }
        IObjectCache ObjectCache { get; }
        Task<bool> PublishMessage(string message, IGameObject target = null, IDictionary<string, string> meta = null);
        Task<bool> PublishMessage(InteriorMessage message, IGameObject target = null);
        Task<bool> PlayerCommand(GamePlayer player, CommandRequest command);
        Task<GamePlayer> GetPlayerByName(string name);
        Task<GamePlayer> GetRootPlayer();
        Task<GameRoom> GetRootRoom();
        Task<GameRoom> GetStartRoom();
        Task<IGameObject> GetObjectById(ObjectId id, GameObjectType? assertType = null);
        Task<T> GetObjectById<T>(ObjectId id) where T : IGameObject;
        Task<IEnumerable<IGameObject>> GetObjectsById(IEnumerable<ObjectId> ids);
        Task<IEnumerable<T>> GetObjectsById<T>(IEnumerable<ObjectId> ids) where T : IGameObject;
        Task<uint> GetActiveServers();
        Task<IEnumerable<ObjectId>> GetActiveRoomIds();
        Task<IEnumerable<ObjectId>> GetConnectedPlayerIds();
        Task InvalidateScriptCache();
        ObjectUpdateObservable WorldEventStream { get; }
        Task FireObjectEvent<T>(ObjectId id, string eventName, T meta, bool localOnly = false) where T : IObjectUpdateResult;
        Task FirePlayerEvent<T>(ObjectId id, string eventName, T meta, bool localOnly = false) where T : IPlayerUpdateResult;
    }
}
