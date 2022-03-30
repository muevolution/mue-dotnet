using System;
using System.Threading.Tasks;
using Mue.Common.Models;

namespace Mue.Common.ClientServer
{
    /// <summary>Commands that can be sent unprompted from a client to the server.</summary>
    public interface IClientCommands
    {
        /// <summary>Client intent: I am requesting authentication.</summary>
        Task<OperationResponse> Auth(AuthRequest data);
        /// <summary>Client intent: I am sending a command for the server to execute.</summary>
        Task<OperationResponse> Command(CommandRequest request);
        /// <summary>Client intent: Echo test.</summary>
        Task<OperationResponse> Echo(string message);
        /// <summary>Client intent: I am going to close the session.</summary>
        Task<OperationResponse> Disconnect();
    }

    /// <summary>Commands that can be sent unprompted from the server to a client.</summary>
    public interface IServerCommands
    {
        /// <summary>Server intent: You have connected to the server.</summary>
        Task Welcome(string motd);
        /// <summary>Server intent: I am sending you a message to display to the user.</summary>
        Task Message(CommunicationsMessage message, MueCodes code);
        /// <summary>Server intent: I am responding to your echo test.</summary>
        Task Echo(string message);
        /// <summary>Server intent: I am going to close the session.</summary>
        Task Disconnect(string reason = null);
        /// <summary>Server intent: A fatal error has occured and the session will end.</summary>
        Task Fatal(string message, MueCodes code);
    }

    public enum MueCodes
    {
        /// <summary>Nothing has happened.</summary>
        Nothing = 0,
        /// <summary>Operation success.</summary>
        Success = 1,
        /// <summary>An unknown error has occured.</summary>
        UnknownError = 2,
        /// <summary>User login failed.</summary>
        LoginError = 100,
        /// <summary>Operation attempted while unauthenticated.</summary>
        UnauthenticatedError = 101,
        /// <summary>PubSub system error.</summary>
        PubSubError = 201,
    }

    public record OperationResponse
    {
        public bool Success { get; set; } = true;
        public bool Fatal { get; set; } = false;
        public string Message { get; set; }
        public MueCodes Code { get; set; } = MueCodes.Success;
    }
}
