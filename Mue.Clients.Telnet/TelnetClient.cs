using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Mue.Clients.ClientServer;
using Mue.Common.Models;

namespace Mue.Clients.Telnet
{
    public class TelnetClient
    {
        private readonly string _hubUrl;

        public TelnetClient(string hubUrl)
        {
            _hubUrl = hubUrl;
        }

        public async Task StartListening(int port, CancellationTokenSource cts)
        {
            var listener = new TcpListener(IPAddress.Any, port);

            try
            {
                listener.Start();
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await AcceptClientAsync(listener, cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Top level client networking error " + e);
                    }
                }
            }
            finally
            {
                cts.Cancel();
                listener.Stop();
            }
        }

        private async Task AcceptClientAsync(TcpListener listener, CancellationToken serverCt)
        {
            using (var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false))
            using (var netStream = new NetworkStream(client.Client))
            using (var bufStream = new BufferedStream(netStream))
            using (var reader = new StreamReader(netStream))
            using (var writer = new StreamWriter(netStream) { AutoFlush = true })
            {
                try
                {
                    await writer.WriteLineAsync("SYS> Connected to telnet bridge");

                    var clientCt = new CancellationTokenSource();
                    var sessionState = new AuthManager();
                    var commandHandler = new CommandHandler(sessionState, writer);
                    await using (var cliSession = new HubClientSession(_hubUrl, commandHandler))
                    {
                        while (!serverCt.IsCancellationRequested && !commandHandler.CancellationToken.IsCancellationRequested)
                        {
                            sessionState.CurrentSession = cliSession;

                            if (!cliSession.IsConnected)
                            {
                                await cliSession.Open();
                            }

                            string line = String.Empty;

                            try
                            {
                                line = await reader.ReadLineAsync();
                                Console.WriteLine("Saw line from client: " + line);
                                await ProcessLine(cliSession, sessionState, line, writer);
                            }
                            catch (Exception e)
                            {
                                // TODO: Log this somewhere
                                Console.WriteLine($"Error in command [{line}] " + e);
                                await writer.WriteLineAsync("The server returned an error while processing your command.");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // TODO: Log this somewhere
                    Console.WriteLine("Error in connection " + e);
                    await writer.WriteLineAsync("Fatal error! " + e.Message);
                }
            }
        }

        private async Task ProcessLine(HubClientSession cliSession, AuthManager sessionState, string line, StreamWriter writer)
        {
            if (!sessionState.IsAuthenticated)
            {
                if (line.StartsWith("auth ") || line.StartsWith("connect "))
                {
                    // Authenticate the user
                    var spl = line.Split(" ");
                    if (spl.Length < 3)
                    {
                        await writer.WriteLineAsync("TS> Not enough arguments for connect");
                        return;
                    }

                    var authRequest = new AuthRequest { Username = spl[1], Password = spl[2] };
                    await sessionState.PerformAuthentication(writer, authRequest);
                }
                else if (line.StartsWith("register "))
                {
                    // Register a new user
                    var spl = line.Split(" ");
                    if (spl.Length < 3)
                    {
                        await writer.WriteLineAsync("TS> Not enough arguments for register");
                        return;
                    }

                    var res = await cliSession.Client.Auth(new AuthRequest { IsRegistration = true, Username = spl[1], Password = spl[2] });
                    if (res.Fatal)
                    {
                        throw new Exception("Command threw fatal: " + res.Message);
                    }
                    else if (!res.Success)
                    {
                        await writer.WriteLineAsync("TS> Registration failed. " + res.Message);
                    }
                }
                else if (line.StartsWith("quit"))
                {
                    // Quit
                    await writer.WriteLineAsync("TS> Goodbye.");
                    await cliSession.Client.Disconnect();
                }
                else
                {
                    await writer.WriteLineAsync("TS> Unknown command");
                }
            }
            else
            {
                var res = await cliSession.Client.Command(new CommandRequest { Command = line });
                if (res.Fatal)
                {
                    throw new Exception("Command threw fatal: " + res.Message);
                }
            }
        }
    }
}