using System.Collections.Generic;
using Mue.Server.Core.ClientServer;

namespace Mue.Server.Hubs
{
    public class MueConnectionManager
    {
        // TODO: There should be a timeout system tied to this
        // Not sure if we can trust removing purely through OnDisconnectedAsync

        public MueConnectionManager()
        {
            this.Connections = new Dictionary<string, MueConnection>();
        }

        public Dictionary<string, MueConnection> Connections { get; private set; }
    }

    public class MueConnection
    {
        public MueConnection(string connectionId, IClientToServer server, IServerToClient client)
        {
            this.ConnectionId = connectionId;
            this.Server = server;
            this.Client = client;
        }

        public string ConnectionId { get; private init; }
        public IClientToServer Server { get; private init; }
        public IServerToClient Client { get; private init; }
    }
}
