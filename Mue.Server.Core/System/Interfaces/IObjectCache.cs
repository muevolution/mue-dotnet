using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mue.Server.Core.Models;
using Mue.Server.Core.Objects;

namespace Mue.Server.Core.System
{
    public interface IObjectCache
    {
        IGameObject GetObject(ObjectId id);
        T GetObject<T>(ObjectId id) where T : class, IGameObject;
        bool HasObjectId(ObjectId id);
        Task<T> StandardCreate<T>(T obj) where T : IGameObject;
        Task<T> StandardImitate<T>(ObjectId id, Func<ObjectMetadata, Task<T>> builder) where T : class, IGameObject<ObjectMetadata>;
        Task<T> StandardImitate<T, MD>(ObjectId id, Func<MD, Task<T>> builder) where T : class, IGameObject<MD> where MD : ObjectMetadata;
        Task<bool> Invalidate(ObjectId id);
        Task<bool> InvalidateLocal(ObjectId id);
        Task<IReadOnlyDictionary<ObjectId, bool>> InvalidateAll(GameObjectType type);
        Task<IReadOnlyDictionary<ObjectId, bool>> InvalidateAll<T>() where T : IGameObject;
        Task OnDestroy(IGameObject obj);
        void PostNetworkDestroy(ObjectId id);
    }
}