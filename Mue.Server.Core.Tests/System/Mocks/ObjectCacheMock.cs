using Mue.Server.Core.System;

public static class ObjectCacheMock
{
    public static Mock<IObjectCache> CreateMock()
    {
        var objectCache = new Mock<IObjectCache>();

        objectCache.BuildFromGameType<GameAction, ActionMetadata>();
        objectCache.BuildFromGameType<GameItem>();
        objectCache.BuildFromGameType<GamePlayer, PlayerMetadata>();
        objectCache.BuildFromGameType<GameRoom>();
        objectCache.BuildFromGameType<GameScript>();

        return objectCache;
    }

    public static void BuildFromGameType<T>(this Mock<IObjectCache> objCacheMock) where T : class, IGameObject<ObjectMetadata>
    {
        objCacheMock.BuildStandardCreate<T>();
        objCacheMock.BuildStandardImitate<T>(new ObjectMetadata { Name = "Sample name", Creator = new ObjectId("p:creatorid"), Parent = new ObjectId("r:parentid"), Location = new ObjectId("r:locationid") });
    }

    public static void BuildFromGameType<T, MD>(this Mock<IObjectCache> objCacheMock) where T : class, IGameObject<MD> where MD : ObjectMetadata, new()
    {
        objCacheMock.BuildStandardCreate<T, MD>();
        objCacheMock.BuildStandardImitate<T, MD>(new MD { Name = "Sample name", Creator = new ObjectId("p:creatorid"), Parent = new ObjectId("r:parentid"), Location = new ObjectId("r:locationid") });
    }

    public static void BuildStandardCreate<T>(this Mock<IObjectCache> objCacheMock) where T : class, IGameObject<ObjectMetadata>
    {
        objCacheMock.Setup(s => s.StandardCreate<T>(It.IsAny<T>()))
            .ReturnsAsync((T obj) => obj);
    }

    public static void BuildStandardCreate<T, MD>(this Mock<IObjectCache> objCacheMock) where T : class, IGameObject<MD> where MD : ObjectMetadata
    {
        objCacheMock.Setup(s => s.StandardCreate<T>(It.IsAny<T>()))
            .ReturnsAsync((T obj) => obj);
    }

    public static void BuildStandardImitate<T>(this Mock<IObjectCache> objCacheMock, ObjectMetadata meta) where T : class, IGameObject<ObjectMetadata>
    {
        objCacheMock.Setup(s => s.StandardImitate<T>(It.IsAny<ObjectId>(), It.IsAny<Func<ObjectMetadata, Task<T>>>()))
            .Returns((ObjectId id, Func<ObjectMetadata, Task<T>> builder) => builder(meta));
    }

    public static void BuildStandardImitate<T, MD>(this Mock<IObjectCache> objCacheMock, MD meta) where T : class, IGameObject<MD> where MD : ObjectMetadata
    {
        objCacheMock.Setup(s => s.StandardImitate<T, MD>(It.IsAny<ObjectId>(), It.IsAny<Func<MD, Task<T>>>()))
            .Returns((ObjectId id, Func<MD, Task<T>> builder) => builder(meta));
    }
}
