using System.Threading.Tasks;
using Mue.Common.ClientServer;
using Mue.Common.Models;

namespace Mue.Server.Core.ClientServer
{
    /// <summary>Commands that can be sent unprompted from a client to the server.</summary>
    public interface IClientToServer
    {
        bool IsConnected { get; }

        /// <summary>Client intent: I am connecting to the server.</summary>
        Task OnConnect(IServerToClient client);
        /// <summary>Client intent: I am requesting authentication.</summary>
        Task<OperationResponse> OnAuthRequest(AuthRequest data);
        /// <summary>Client intent: I am sending a command for the server to execute.</summary>
        Task<OperationResponse> OnCommand(CommandRequest request);
        /// <summary>Client intent: Echo test.</summary>
        Task<OperationResponse> OnEcho(string message);
        /// <summary>Client intent: I am going to close the session.</summary>
        Task<OperationResponse> OnDisconnect();
    }
}
