using System;
using Moq;

namespace Mue.Backend.Storage
{
    public class BackendStorageMock
    {
        public BackendStorageMock()
        {
            Transact = CreateTransaction();
            Storage = CreateMock();
        }

        public Mock<IBackendStorage> Storage { get; private init; }
        public Mock<IBackendStorageTransaction> Transact { get; private init; }

        private Mock<IBackendStorage> CreateMock()
        {
            var mock = new Mock<IBackendStorage>();
            mock.Setup(s => s.StartTransaction()).Returns(Transact.Object);
            SetupOps(mock);
            return mock;
        }

        private Mock<IBackendStorageTransaction> CreateTransaction()
        {
            var mock = new Mock<IBackendStorageTransaction>();
            SetupOps(mock);
            return mock;
        }

        private void SetupOps(IMock<IBackendStorageOperations> mock)
        {
            // Currently we don't have any defaults
        }
    }
}