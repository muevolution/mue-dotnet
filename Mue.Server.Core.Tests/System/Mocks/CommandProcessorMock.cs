using System;
using Moq;
using Mue.Server.Core.System;

namespace Mue.Server.Core.Tests
{
    public static class CommandProcessorMock
    {
        public static Mock<ICommandProcessor> CreateMock()
        {
            var commandProc = new Mock<ICommandProcessor>();
            return commandProc;
        }
    }
}