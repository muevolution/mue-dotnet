using System.Text;

public static class MOTD
{
    public const string StaticMOTD = @"Welcome to mue (multi-user evolution)! This system is still under development.

  █▀▄▀█   ▄   █      ▄▄▄▄▀ ▄█   ▄      ▄▄▄▄▄   ▄███▄   █▄▄▄▄
  █ █ █    █  █   ▀▀▀ █    ██    █    █     ▀▄ █▀   ▀  █  ▄▀
  █ ▄ █ █   █ █       █    ██ █   █ ▄  ▀▀▀▀▄   ██▄▄    █▀▀▌
  █   █ █   █ ███▄   █     ▐█ █   █  ▀▄▄▄▄▀    █▄   ▄▀ █  █
     █  █▄ ▄█     ▀ ▀       ▐ █▄ ▄█            ▀███▀     █
    ▀    ▀▀▀                   ▀▀▀                      ▀
  ▄███▄      ▄   ████▄ █       ▄     ▄▄▄▄▀ ▄█ ████▄    ▄
  █▀   ▀      █  █   █ █        █ ▀▀▀ █    ██ █   █     █
  ██▄▄   █     █ █   █ █     █   █    █    ██ █   █ ██   █
  █▄   ▄▀ █    █ ▀████ ███▄  █   █   █     ▐█ ▀████ █ █  █
  ▀███▀    █  █            ▀ █▄ ▄█  ▀       ▐       █  █ █
            █▐                ▀▀▀                   █   ██
            ▐

          🚧 This is a development server. Help us develop! 🚧
               https://github.com/muevolution/mue-dotnet
";

    public static async Task<string> GetLiveMOTD(IWorld _world)
    {
        var sb = new StringBuilder(StaticMOTD);

        if ((await _world.StorageManager.GetRootValue(RootField.God)) == null)
        {
            sb.AppendLine();
            sb.AppendLine("  ⚠️ NOTICE: This server has not yet been initialized! ⚠️  ");
            sb.AppendLine("  ⚠️ Run `Mue.Server.Tools.exe --task init`            ⚠️  ");
        }

        return sb.ToString();
    }
}