using System;
using Moq;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Tests
{
    public static class WorldMock
    {
        public static Mock<IWorld> CreateMock(IObjectCache objectCache, IStorageManager storageManager)
        {
            var world = new Mock<IWorld>();

            world.Setup(s => s.ObjectCache).Returns(objectCache);
            world.Setup(s => s.StorageManager).Returns(storageManager);

            return world;
        }

    }
}