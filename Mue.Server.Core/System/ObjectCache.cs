using System.Collections.Concurrent;

namespace Mue.Server.Core.System;

public class ObjectCache : IObjectCache
{
    private readonly IWorld _world;
    private readonly IDictionary<ObjectId, IGameObject> _cache = new ConcurrentDictionary<ObjectId, IGameObject>();

    public ObjectCache(IWorld world)
    {
        _world = world;
    }

    public ObjectCache(IWorld world, IDictionary<ObjectId, IGameObject> precache)
    {
        _world = world;
        _cache = precache;
    }

    public IGameObject? GetObject(ObjectId id)
    {
        if (!HasObjectId(id))
        {
            return null;
        }

        return _cache[id];
    }

    public T? GetObject<T>(ObjectId id) where T : class, IGameObject
    {
        if (!HasObjectId(id))
        {
            return null;
        }

        var obj = _cache[id];

        if (obj.Id.ObjectType != GameObjectConsts.GetGameObjectType<T>())
        {
            // If we asked for the wrong type just return null
            // We might want to change this to throw?
            return null;
        }

        return (T)obj;
    }

    public bool HasObjectId(ObjectId id)
    {
        return _cache.ContainsKey(id);
    }

    public async Task<T> StandardCreate<T>(T obj) where T : IGameObject
    {
        if (HasObjectId(obj.Id))
        {
            throw new GameObjectIdExistsException(obj.Id);
        }

        await _world.StorageManager.AddObject(obj);
        PutObject(obj);

        return obj;
    }

    public Task<T> StandardImitate<T>(ObjectId id, Func<ObjectMetadata, Task<T>> builder) where T : class, IGameObject<ObjectMetadata>
    {
        return StandardImitate<T, ObjectMetadata>(id, builder);
    }

    public async Task<T> StandardImitate<T, MD>(ObjectId id, Func<MD, Task<T>> builder) where T : class, IGameObject<MD> where MD : ObjectMetadata
    {
        var expected = GameObjectConsts.GetGameObjectType<T>();
        if (id.ObjectType != expected)
        {
            throw new GameObjectTypeDoesNotMatchException(id, expected);
        }

        var cachedObj = GetObject<T>(id);
        if (cachedObj != null)
        {
            return cachedObj;
        }

        var meta = await _world.StorageManager.GetMeta<MD>(id);
        if (meta == null)
        {
            throw new GameObjectIdDoesNotExistException(id);
        }

        var obj = await builder(meta);
        PutObject(obj);
        return obj;
    }

    public async Task<bool> Invalidate(ObjectId id)
    {
        await _world.FireObjectEvent(id, ObjectUpdate.EVENT_INVALIDATE, EmptyObjectUpdateResult.Empty);
        return await InvalidateLocal(id);
    }

    public async Task<bool> InvalidateLocal(ObjectId id)
    {
        var obj = GetObject(id);
        if (obj != null)
        {
            return await obj.Invalidate();
        }

        return false;
    }

    public async Task<IReadOnlyDictionary<ObjectId, bool>> InvalidateAll(GameObjectType type)
    {
        var allOfType = _cache.Keys.Where(s => s.ObjectType == type).Select(async id => (id, await InvalidateLocal(id)));
        var result = await Task.WhenAll(allOfType);
        return result.ToDictionary(s => s.id, s => s.Item2);
    }

    public Task<IReadOnlyDictionary<ObjectId, bool>> InvalidateAll<T>() where T : IGameObject
    {
        return InvalidateAll(GameObjectConsts.GetGameObjectType<T>());
    }

    /// <summary>Start the destroy process for an object, sent to the network.</summary>
    public async Task OnDestroy(IGameObject obj)
    {
        if (!obj.IsDestroyed)
        {
            // TODO: Specific type
            throw new Exception("Game object was not destroyed");
        }

        await _world.FireObjectEvent(obj.Id, ObjectUpdate.EVENT_DESTROY, EmptyObjectUpdateResult.Empty);
    }

    /// <summary>Remove the destroyed object from this server.</summary>
    public void PostNetworkDestroy(ObjectId id)
    {
        if (_cache.ContainsKey(id))
        {
            _cache.Remove(id);
        }
    }

    private void PutObject(IGameObject obj)
    {
        if (obj.IsPendingAdd)
        {
            throw new GameObjectIdDoesNotExistException(obj.Id);
        }

        _cache.Add(obj.Id, obj);
    }
}
