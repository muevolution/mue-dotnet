using Microsoft.Extensions.DependencyInjection;

namespace Mue.Server.Core.System;

public interface ISystemFactory
{
    ICommandProcessor CommandProcessor { get; }
    IObjectCache ObjectCache { get; }
    IStorageManager StorageManager { get; }
}

public class SystemFactory : ISystemFactory
{
    private IServiceProvider _serviceProvider;

    public SystemFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICommandProcessor CommandProcessor => _serviceProvider.GetRequiredService<ICommandProcessor>();
    public IObjectCache ObjectCache => _serviceProvider.GetRequiredService<IObjectCache>();
    public IStorageManager StorageManager => _serviceProvider.GetRequiredService<IStorageManager>();
}
