using System;
using System.IO;
using System.Threading.Tasks;
using Mue.Clients.ClientServer;
using Mue.Common.Models;

namespace Mue.Clients.Telnet
{
    public class AuthManager
    {
        private AuthRequest? _cachedState;

        public HubClientSession? CurrentSession { get; set; }
        public bool IsAuthenticated { get; private set; }
        public bool ShouldReauth => IsAuthenticated && CurrentSession != null;

        public void OnAuthenticated(AuthRequest cachedState)
        {
            _cachedState = cachedState;
            this.IsAuthenticated = true;
        }

        public void ClearAuth()
        {
            this.IsAuthenticated = false;
        }

        public async Task<bool> PerformAuthentication(StreamWriter writer, AuthRequest? authRequest = null)
        {
            if (CurrentSession?.Client == null)
            {
                throw new InvalidOperationException();
            }

            var req = authRequest ?? _cachedState ?? throw new ArgumentNullException("authRequest");
            var res = await CurrentSession.Client.Auth(req);
            if (res.Fatal)
            {
                throw new Exception("Command threw fatal: " + res.Message);
            }
            else if (!res.Success)
            {
                await writer.WriteLineAsync("AUTH> Authentication failed. " + res.Message);
                return false;
            }

            // Assume we've authenticated
            await writer.WriteLineAsync("AUTH> Authentication succeeded!");
            _cachedState = authRequest;
            this.IsAuthenticated = true;

            return true;
        }
    }
}