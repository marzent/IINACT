namespace IINACT.Network;

internal static class GameServerTime
{
    public static long LastSeverTimestamp { get; private set; }
    
    private static long LastSeverTimestampTicks { get; set; }
    
    private static readonly DateTime Date1970 = DateTime.MinValue.AddYears(1969);

    public static DateTime LastServerTime => LastSeverTimestamp > 0
                                                 ? Date1970.AddTicks((long)LastSeverTimestamp * 10_000L).ToLocalTime()
                                                 : DateTime.Now;
    
    public static DateTime CurrentServerTime => LastSeverTimestamp > 0
                                                    ? LastServerTime.AddMilliseconds(
                                                        Environment.TickCount64 - LastSeverTimestampTicks)
                                                    : DateTime.Now;

    internal static void SetLastServerTimestamp(ulong timestamp)
    {
        if (timestamp > 0)
        {
            LastSeverTimestamp = (long)timestamp;
            LastSeverTimestampTicks = Environment.TickCount64;
        }
    }
}
