namespace Mue.Server.Core.Objects;

public abstract class Container<MD> : GameObject<MD>, IContainer where MD : ObjectMetadata
{
    protected bool _useDeepSearch { get; init; }

    protected Container(IWorld world, GameObjectType objType, MD meta, ObjectId? id = null) : base(world, objType, meta, id) { }

    public Task<IEnumerable<ObjectId>> GetContents(GameObjectType? objType = null) => _world.StorageManager.GetContents(this.Id, objType);

    public virtual Task<ObjectId?> Find(string term, GameObjectType? type = null) => FindIn(term, type);

    public async Task<ObjectId?> FindIn(string term, GameObjectType? type = null)
    {
        var contentIds = await GetContents();
        if (contentIds.Count() < 1)
        {
            return null;
        }

        var contents = (await _world.GetObjectsById(contentIds)).WhereNotNull();

        if (type == GameObjectType.Action)
        {
            var actions = contents.WhereObjectType<GameAction>();
            var matchedAction = actions.SingleOrDefault(s => s.MatchCommand(term));
            if (matchedAction != null)
            {
                return matchedAction.Id;
            }
        }

        // Test general item names
        var inv = contents.FirstOrDefault(s => (!type.HasValue || type == s.ObjectType) && s.MatchName(term));
        if (inv != null)
        {
            return inv.Id;
        }

        if (!_useDeepSearch)
        {
            return null;
        }

        // Test contained item's actions
        var items = contents.WhereObjectType<GameItem>();
        foreach (var item in items)
        {
            var fi = await item.FindIn(term, type);
            if (fi != null)
            {
                return fi;
            }
        }

        return null;
    }

    public override async Task<bool> Destroy()
    {
        if (!await SpillContents())
        {
            // TODO: Throw error or something
            return false;
        }

        return await base.Destroy();
    }

    private async Task<bool> SpillContents()
    {
        // TODO: This will need to reparent all the children somehow too
        var newParent = this.Location ?? this.Parent;
        var contents = (await this.GetContents()).ToList();
        if (contents.Count < 1)
        {
            return true;
        }

        // Move all the objects in a transaction
        // TODO: Traditionally this sends everything 'home', should we dump to parent instead?
        var result = await _world.StorageManager.MoveObjects(contents, newParent, this.Id);
        if (!result)
        {
            return false;
        }

        // Update all the objects with their new container
        var objects = await Task.WhenAll(contents.Select(s => _world.GetObjectById(s)));
        objects.WhereNotNull().ToList().ForEach(f => f.MoveFinish(newParent, this.Id));

        return true;
    }
}

public abstract class Container : Container<ObjectMetadata>
{
    protected Container(IWorld world, GameObjectType objType, ObjectMetadata meta, ObjectId? id = null) : base(world, objType, meta, id) { }
}

public static class ContainerExtensions
{
    public static Task<IEnumerable<ObjectId>> GetContents<T>(this IContainer container) where T : IGameObject
    {
        return container.GetContents(GameObjectConsts.GetGameObjectType<T>());
    }

    public static Task<ObjectId?> Find<T>(this IContainer container, string term) where T : IGameObject
    {
        return container.Find(term, GameObjectConsts.GetGameObjectType<T>());
    }

    public static Task<ObjectId?> FindIn<T>(this IContainer container, string term) where T : IGameObject
    {
        return container.FindIn(term, GameObjectConsts.GetGameObjectType<T>());
    }
}
