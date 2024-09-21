namespace Machina.FFXIV.Dalamud
{
    public static class NotDeucalionInjector
    {
        public static string LastInjectionError
        {
            get
            {
                return string.Empty;
            }
            set
            {
                // Do nothing
            }
        }

        public static bool ValidateLibraryChecksum()
        {
            return true;
        }

        public static string ExtractLibrary()
        {
            return string.Empty;
        }

        public static bool InjectLibrary(int processId)
        {
            return true;
        }
    }
}
