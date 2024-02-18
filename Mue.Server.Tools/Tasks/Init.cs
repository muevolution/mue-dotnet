using Mue.Backend;
using Mue.Server.Core.Objects;

namespace Mue.Server.Tools;

public class InitTask : IStandardTask
{
    private readonly ILogger<InitTask> _logger;
    private readonly IWorld _world;
    private readonly RedisBackend _backend;

    public InitTask(ILogger<InitTask> logger, IWorld world, RedisBackend backend)
    {
        _logger = logger;
        _world = world;
        _backend = backend;
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        var srvCount = await _world.GetActiveServers();
        if (srvCount > 1)
        {
            throw new Exception("A server is connected!");
        }

        // Start from scratch
        await _backend.FlushDb();

        // Players
        var player1 = await GamePlayer.RootCreate(_world, "Hera");
        await _world.StorageManager.SetRootValue(RootField.God, player1.Id.Id);
        _logger.LogDebug("Player 1 is " + player1);

        // Rooms
        var room0 = await GameRoom.RootCreate(_world, "#0");
        await _world.StorageManager.SetRootValue(RootField.RootRoom, room0.Id.Id);
        await _world.StorageManager.SetRootValue(RootField.StartRoom, room0.Id.Id);

        var playerRootRoom = await GameRoom.Create(_world, "Player root", player1.Id, room0.Id);
        await _world.StorageManager.SetRootValue(RootField.PlayerRoot, playerRootRoom.Id.Id);

        _logger.LogDebug("Rooms are: " + room0 + " + " + playerRootRoom);

        // Put players in room
        await player1.Move(room0.Id);
        await player1.Reparent(playerRootRoom.Id);
        _logger.LogDebug("Player moves complete");

        // Create second player in one go
        var player2 = await GamePlayer.Create(_world, "Kauko", "kaukopasswd", player1.Id, playerRootRoom.Id, room0.Id);
        _logger.LogDebug("Player 2 is: " + player2);

        // Load scripts
        var scriptLoader = new ScriptLoader(_world);
        await scriptLoader.UpdateScripts(player1.Id, room0.Id, room0.Id);

        _logger.LogInformation("Server init complete.");
    }
}
