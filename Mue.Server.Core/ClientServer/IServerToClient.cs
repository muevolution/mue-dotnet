using System.Threading.Tasks;
using Mue.Common.ClientServer;
using Mue.Common.Models;

namespace Mue.Server.Core.ClientServer
{
    /// <summary>Commands that can be sent unprompted from the server to a client.</summary>
    public interface IServerToClient
    {
        /// <summary>Server intent: You have connected to the server.</summary>
        Task SendWelcome(string motd);
        // Task SendAuthResponse(AuthResponse data, MueCodes code);
        /// <summary>Server intent: I am sending you a message to display to the user.</summary>
        Task SendMessage(CommunicationsMessage message, MueCodes code);
        /// <summary>Server intent: I am responding to your echo test.</summary>
        Task SendEcho(string message);
        /// <summary>Server intent: I am going to close the session.</summary>
        Task SendDisconnect(string reason = null);
        /// <summary>Server intent: A fatal error has occured and the session will end.</summary>
        Task SendFatal(string message, MueCodes code);
    }
}
