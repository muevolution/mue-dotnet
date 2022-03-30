using System;
using Moq;
using Mue.Backend.PubSub;

namespace Mue.Backend.PubSub
{
    public class BackendPubSubMock
    {
        public static Mock<IBackendPubSub> CreateMock()
        {
            var mock = new Mock<IBackendPubSub>();
            return mock;
        }
    }
}