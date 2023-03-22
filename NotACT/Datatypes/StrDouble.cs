namespace Advanced_Combat_Tracker;

public class StrDouble : IComparable, IEquatable<StrDouble>
{
    public StrDouble(string Name, double Val)
    {
        this.Name = Name;
        this.Val = Val;
    }

    public string Name { get; }

    public double Val { get; }

    public int CompareTo(object? obj)
    {
        if (obj is not StrDouble other)
            throw new ArgumentException("Object is not a StrDouble.");

        return Val.CompareTo(other.Val);
    }


    public bool Equals(StrDouble? other)
    {
        return Name == other!.Name && Val == other!.Val;
    }
}
