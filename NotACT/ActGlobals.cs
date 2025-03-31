namespace Advanced_Combat_Tracker;

public static partial class ActGlobals
{
    private static ActLocalization.LocalizationStringsHelper? _trans;

    public static bool mainTableShowCommas = true;

    public static bool calcRealAvgDelay = true;

    internal static SortedDictionary<string, bool> selectiveList;

    public static bool longDuration = false;

    public static bool blockIsHit = true;

    public static bool restrictToAll = false;

    public static string charName = "YOU";

    public static string eDSort = "EncDPS";

    public static string mDSort = "Damage";

    public static string aTSort = "Time";

    public static string eDSort2 = "EncDPS";

    public static string mDSort2 = "Damage";

    public static string aTSort2 = "Time";

    public static FormActMain oFormActMain = null!;

    internal static object ActionDataLock;

    internal static ActLocalization.LocalizationStringsHelper Trans => _trans!;
    
    public static void Init()
    {
        _trans = new ActLocalization.LocalizationStringsHelper();
        selectiveList = new SortedDictionary<string, bool>();
        ActionDataLock = new object();
    }
    
    public static void Dispose()
    {
        oFormActMain.Exit();
        oFormActMain.Dispose();
        oFormActMain = null!;
        _trans = null!;
        selectiveList.Clear();
        selectiveList = null!;
        ActionDataLock = null!;
    }
}
