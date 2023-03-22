namespace Advanced_Combat_Tracker;

public class LocalizationObject
{
    public LocalizationObject(string DisplayedText, string LocalizationDescription)
    {
        this.DisplayedText = DisplayedText;
        this.LocalizationDescription = LocalizationDescription;
    }

    public string DisplayedText { get; set; }

    public string LocalizationDescription { get; }

    internal string S => DisplayedText;

    public override string ToString() => DisplayedText;

    public static implicit operator string(LocalizationObject val) => val.DisplayedText;

    public static implicit operator LocalizationObject(string val) => 
        new LocalizationObject(val, string.Empty);
}
