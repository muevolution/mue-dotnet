namespace Mue.Server.Core.Utils;

public static class TimeUtil
{
    private static readonly DateTime StaticTime = new DateTime(2020, 3, 12, 0, 0, 0, DateTimeKind.Utc);
    public static bool InTestMode { get; set; }

    public static DateTime MueNow
    {
        get
        {
            if (InTestMode)
            {
                return StaticTime.ToUniversalTime();
            }
            else
            {
                return DateTime.UtcNow;
            }
        }
    }

    public static string FrozenTimeString => StaticTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK");
}
