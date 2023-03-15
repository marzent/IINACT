namespace Advanced_Combat_Tracker
{
    public class LocalizationObject
    {
        public string DisplayedText { get; set; }

        public string LocalizationDescription { get; }

        internal string S => DisplayedText;

        public LocalizationObject(string DisplayedText, string LocalizationDescription)
        {
            this.DisplayedText = DisplayedText;
            this.LocalizationDescription = LocalizationDescription;
        }

        public override string ToString()
        {
            return DisplayedText;
        }

        public static implicit operator string(LocalizationObject val)
        {
            return val.DisplayedText;
        }

        public static implicit operator LocalizationObject(string val)
        {
            return new LocalizationObject(val, string.Empty);
        }
    }
}
