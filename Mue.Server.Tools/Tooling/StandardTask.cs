using System.Threading;
using System.Threading.Tasks;

namespace Mue.Server.Tools
{
    public interface StandardTask
    {
        Task Start(CancellationToken cancellationToken);
    }
}