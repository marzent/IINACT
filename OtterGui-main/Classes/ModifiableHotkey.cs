using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Newtonsoft.Json;

namespace OtterGui.Classes;

// A wrapper to combine a single regular key with up to two modifier keys.
public struct ModifiableHotkey : IEquatable<ModifiableHotkey>
{
    public VirtualKey     Hotkey    { get; private set; } = VirtualKey.NO_KEY;
    public ModifierHotkey Modifier1 { get; private set; } = ModifierHotkey.NoKey;
    public ModifierHotkey Modifier2 { get; private set; } = ModifierHotkey.NoKey;

    public ModifiableHotkey()
    { }

    public ModifiableHotkey(VirtualKey hotkey, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
    }

    public ModifiableHotkey(VirtualKey hotkey, ModifierHotkey modifier1, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
        SetModifier1(modifier1);
    }

    [JsonConstructor]
    public ModifiableHotkey(VirtualKey hotkey, ModifierHotkey modifier1, ModifierHotkey modifier2, VirtualKey[]? validKeys = null)
    {
        SetHotkey(hotkey, validKeys);
        SetModifier1(modifier1);
        SetModifier2(modifier2);
    }

    // Try to set the given hotkey.
    // If validKeys is given, the hotkey has to be contained in it.
    // If the key is empty, both modifiers will be reset.
    // Returns true if any change took place.
    public bool SetHotkey(VirtualKey key, IReadOnlyList<VirtualKey>? validKeys = null)
    {
        if (Hotkey == key || validKeys != null && !validKeys.Contains(key))
            return false;

        if (key == VirtualKey.NO_KEY)
        {
            Modifier1 = VirtualKey.NO_KEY;
            Modifier2 = VirtualKey.NO_KEY;
        }

        Hotkey = key;
        return true;
    }

    // Try to set the first modifier.
    // If the modifier is empty, the second modifier will be reset.
    // Returns true if any change took place.
    public bool SetModifier1(ModifierHotkey key)
    {
        if (Modifier1 == key)
            return false;

        if (key == ModifierHotkey.NoKey || key == Modifier2)
            Modifier2 = ModifierHotkey.NoKey;

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

        Modifier2 = Modifier1 == key ? ModifierHotkey.NoKey : key;
        return true;
    }

    public bool Equals(ModifiableHotkey other)
        => Hotkey == other.Hotkey
         && Modifier1 == other.Modifier1
         && Modifier2 == other.Modifier2;

    public override bool Equals(object? obj)
        => obj is ModifiableHotkey other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine((int)Hotkey, Modifier1, Modifier2);

    public static bool operator ==(ModifiableHotkey lhs, ModifiableHotkey rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(ModifiableHotkey lhs, ModifiableHotkey rhs)
        => !lhs.Equals(rhs);
}


public static class VirtualKeyExtensions
{
    public static bool IsPressed(this VirtualKey key, KeyState state)
        => state[key];
}