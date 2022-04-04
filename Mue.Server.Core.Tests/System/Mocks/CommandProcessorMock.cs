using Mue.Server.Core.System;

public static class CommandProcessorMock
{
    public static Mock<ICommandProcessor> CreateMock()
    {
        var commandProc = new Mock<ICommandProcessor>();
        return commandProc;
    }
}
