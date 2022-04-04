namespace Mue.Clients.Telnet;

public class CommandHandler : IServerCommands
{
    private readonly AuthManager _sessionState;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly StreamWriter _clientWriter;

    public CommandHandler(AuthManager sessionState, StreamWriter clientWriter)
    {
        _sessionState = sessionState;
        _clientWriter = clientWriter;
    }

    public CancellationToken CancellationToken { get { return _cts.Token; } }

    public async Task Welcome(string motd)
    {
        if (_sessionState.ShouldReauth)
        {
            if (await _sessionState.PerformAuthentication(_clientWriter))
            {
                await _clientWriter.WriteLineAsync("TS> Reconnected to game server");
                return;
            }
            else
            {
                await _clientWriter.WriteLineAsync("AUTH> Failed to reauthenticate with game server, please log in again");
            }
        }

        await _clientWriter.WriteLineAsync("TS> Connected to game server");
        await _clientWriter.WriteLineAsync("MOTD>");
        await _clientWriter.WriteLineAsync(motd);
        await _clientWriter.WriteLineAsync(@"Telnet bridge commands:
  - auth <username> <password>
  - register <username> <password>
");

        _sessionState.ClearAuth();
    }

    public async Task Message(CommunicationsMessage message, MueCodes code)
    {
        await _clientWriter.WriteLineAsync($"[{message.Target}] {message.Message}");
        if (message.Meta != null)
        {
            await _clientWriter.WriteLineAsync($">META> {{{String.Join(",", message.Meta.Select(s => $"{s.Key}: {s.Value}"))}}}");
        }
    }

    public async Task Echo(string message)
    {
        await _clientWriter.WriteLineAsync($"ECHO> {message}");
    }

    public async Task Disconnect(string? reason = null)
    {
        await _clientWriter.WriteLineAsync($"QUIT> {reason ?? ""}");
        _cts.Cancel();
    }

    public async Task Fatal(string message, MueCodes code)
    {
        await _clientWriter.WriteLineAsync($"ERR> Got fatal error {message}");
        _cts.Cancel();
    }
}
