using System;
using Dalamud.Game.ClientState.Keys;
using Newtonsoft.Json;

namespace OtterGui.Classes;

public struct DoubleModifier : IEquatable<DoubleModifier>
{
    public static readonly DoubleModifier NoKey = new();

    public ModifierHotkey Modifier1 { get; private set; } = ModifierHotkey.NoKey;
    public ModifierHotkey Modifier2 { get; private set; } = ModifierHotkey.NoKey;

    public DoubleModifier()
    { }

    public DoubleModifier(ModifierHotkey modifier1)
    {
        SetModifier1(modifier1);
    }

    [JsonConstructor]
    public DoubleModifier(ModifierHotkey modifier1, ModifierHotkey modifier2)
    {
        SetModifier1(modifier1);
        SetModifier2(modifier2);
    }

    // Try to set the first modifier.
    // If the modifier is empty, the second modifier will be reset.
    // Returns true if any change took place.
    public bool SetModifier1(ModifierHotkey key)
    {
        if (Modifier1 == key)
            return false;

        if (key == VirtualKey.NO_KEY || key == Modifier2)
            Modifier2 = VirtualKey.NO_KEY;

        Modifier1 = key;
        return true;
    }

    // Try to set the second modifier.
    // Returns true if any change took place.
    // If the first modifier is already the given key, resets this one instead.
    public bool SetModifier2(ModifierHotkey key)
    {
        if (Modifier2 == key)
            return false;

        Modifier2 = Modifier1 == key ? VirtualKey.NO_KEY : key;
        return true;
    }

    public bool Equals(DoubleModifier other)
        => Modifier1.Equals(other.Modifier1)
         && Modifier2.Equals(other.Modifier2);

    public override bool Equals(object? obj)
        => obj is DoubleModifier other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Modifier1, Modifier2);

    public static bool operator ==(DoubleModifier lhs, DoubleModifier rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(DoubleModifier lhs, DoubleModifier rhs)
        => !lhs.Equals(rhs);

    public override string ToString()
        => Modifier2 != ModifierHotkey.NoKey
            ? $"{Modifier1} and {Modifier2}"
            : Modifier1.ToString();

    public bool IsActive()
        => Modifier1.IsActive() && Modifier2.IsActive();
}
