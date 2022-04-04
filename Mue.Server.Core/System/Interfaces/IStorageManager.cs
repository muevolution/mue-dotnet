using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System
{
    public interface IStorageManager
    {
        Task<bool> AddObject(IGameObject obj);
        Task<bool> DestroyObject(IGameObject obj);
        Task<bool> DoesObjectExist(ObjectId id);
        Task<IReadOnlyDictionary<string, string>> GetAllPlayers();
        Task<string?> FindPlayerByName(string name);
        Task<bool> UpdatePlayerNameIndex(ObjectId id, string oldName, string newName);
        Task<PropValue> GetProp(ObjectId owner, string path);
        Task<IReadOnlyDictionary<string, PropValue>> GetProps(ObjectId owner);
        Task<bool> SetProp(ObjectId owner, string path, PropValue value);
        Task<bool> SetProps(ObjectId owner, IReadOnlyDictionary<string, PropValue> values);
        Task<IEnumerable<ObjectId>> GetContents(ObjectId owner, GameObjectType? type = null);
        Task<bool> ReparentObject(ObjectId objectId, ObjectId newParent, ObjectId? oldParent = null);
        Task<bool> MoveObject(ObjectId objectId, ObjectId newLocation, ObjectId? oldLocation = null);
        Task<bool> MoveObjects(IEnumerable<ObjectId> objectIds, ObjectId newLocation, ObjectId? oldLocation = null);
        Task<MD?> GetMeta<MD>(ObjectId objectId) where MD : ObjectMetadata;
        Task<ObjectMetadata?> GetMeta(ObjectId objectId);
        Task<string?> GetMeta(ObjectId objectId, string key);
        Task<bool> UpdateMeta<MD>(ObjectId objectId, MD meta) where MD : ObjectMetadata;
        Task<bool> UpdateMeta(ObjectId objectId, string key, string value);
        Task<string?> GetRootValue(RootField field);
        Task<bool> SetRootValue(RootField field, string value);
        Task<string?> GetScriptCode(ObjectId objectId);
        Task<bool> SetScriptCode(ObjectId objectId, string code);
    }
}
