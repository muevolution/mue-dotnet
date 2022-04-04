namespace Mue.Server.Tools;

public interface StandardTask
{
    Task Start(CancellationToken cancellationToken);
}
