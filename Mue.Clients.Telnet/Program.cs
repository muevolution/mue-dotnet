using Mue.Clients.Telnet;

var cts = new CancellationTokenSource();

var backendServerRoot = Environment.GetEnvironmentVariable("BACKEND_SERVER_URL") ?? "http://localhost:5000";
var backendServerPath = $"{backendServerRoot}/mueclient";

var telnetClient = new TelnetClient(backendServerPath);
Console.WriteLine("Listening on port 8888");
await telnetClient.StartListening(8888, cts);
