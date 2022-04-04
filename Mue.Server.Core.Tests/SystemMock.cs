using Microsoft.Extensions.DependencyInjection;
using Mue.Backend.PubSub;
using Mue.Backend.Storage;
using Mue.Server.Core.System;

public class SystemMock
{
    public SystemMock()
    {
        BackendStorage = new BackendStorageMock();
        BackendPubSub = BackendPubSubMock.CreateMock();
        ObjectCache = ObjectCacheMock.CreateMock();
        StorageManager = StorageManagerMock.CreateMock();
        CommandProcessor = CommandProcessorMock.CreateMock();
        World = WorldMock.CreateMock(ObjectCache.Object, StorageManager.Object);

        ServiceProvider = CreateServiceProvider();
    }

    public BackendStorageMock BackendStorage { get; private init; }
    public Mock<IBackendPubSub> BackendPubSub { get; private init; }
    public Mock<IObjectCache> ObjectCache { get; private init; }
    public Mock<IStorageManager> StorageManager { get; private init; }
    public Mock<ICommandProcessor> CommandProcessor { get; private init; }
    public Mock<IWorld> World { get; private init; }
    public IServiceProvider ServiceProvider { get; private init; }

    private IServiceProvider CreateServiceProvider()
    {
        var sc = new ServiceCollection();

        sc.AddSingleton<IBackendPubSub>(BackendPubSub.Object);
        sc.AddSingleton<IObjectCache>(ObjectCache.Object);
        sc.AddSingleton<IStorageManager>(StorageManager.Object);
        sc.AddSingleton<ICommandProcessor>(CommandProcessor.Object);
        sc.AddSingleton<ISystemFactory, SystemFactory>();
        sc.AddSingleton<IWorld>(World.Object);

        return sc.BuildServiceProvider();
    }
}
