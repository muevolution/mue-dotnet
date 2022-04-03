using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.Rx;

namespace Mue.Server.Core.Objects
{
    public interface IGameObject<MD> : IGameObject where MD : ObjectMetadata
    {
        MD Meta { get; }
    }

    public interface IGameObject
    {
        ObjectMetadata MetaBasic { get; }
        GameObjectType ObjectType { get; }
        ObjectId Id { get; }
        bool IsPendingAdd { get; }
        bool IsDestroyed { get; }
        void SetInitialId(string newId);
        string Name { get; }
        ObjectId Parent { get; }
        bool IsParentRoot { get; }
        ObjectId Location { get; }
        bool IsLocationRoot { get; }
        Task<PropValue> GetProp(string path);
        Task<IReadOnlyDictionary<string, PropValue>> GetProps();
        Task<bool> SetProp(string path, PropValue value);
        Task<bool> SetProps(IReadOnlyDictionary<string, PropValue> values);
        bool MatchName(string term);
        Task<bool> Rename(string newName);
        Task<ReparentResult> Reparent(ObjectId newParent);
        Task<MoveResult> Move(ObjectId newLocation);
        MoveResult MoveFinish(ObjectId newLocation, ObjectId oldLocation = null);
        Task<bool> Destroy();
        Task<bool> Invalidate();
    }
}
