namespace Mue.Clients.Telnet;

class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();

        var telnetClient = new TelnetClient("http://localhost:5000/mueclient");
        Console.WriteLine("Listening on port 8888");
        await telnetClient.StartListening(8888, cts);
    }
}
