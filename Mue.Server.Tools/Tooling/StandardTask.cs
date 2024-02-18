namespace Mue.Server.Tools;

public interface IStandardTask
{
    Task Start(CancellationToken cancellationToken);
}
