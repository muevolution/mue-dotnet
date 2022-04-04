using Mue.Server.Core.System;

public static class StorageManagerMock
{
    public static Mock<IStorageManager> CreateMock()
    {
        var storageManager = new Mock<IStorageManager>();

        storageManager.Setup(s => s.UpdateMeta(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        storageManager.Setup(s => s.UpdateMeta(It.IsAny<ObjectId>(), It.IsAny<ObjectMetadata>())).ReturnsAsync(true);

        return storageManager;
    }
}
