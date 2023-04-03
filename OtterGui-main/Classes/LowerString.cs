using System;
using ImGuiNET;
using Newtonsoft.Json;

namespace OtterGui.Classes;

// A string that always keeps a lower-cased version of itself
// Does automatically compare case-insensitive to other LowerStrings and regular strings.
[JsonConverter(typeof(Converter))]
public readonly struct LowerString : IEquatable<LowerString>, IComparable<LowerString>, IEquatable<string>, IComparable<string>
{
    public static readonly LowerString Empty = new(string.Empty);

    public readonly string Text  = string.Empty;
    public readonly string Lower = string.Empty;

    public LowerString(string text)
    {
        Text  = string.Intern(text);
        Lower = string.Intern(text.ToLowerInvariant());
    }

    public int Length
        => Text.Length;

    public int Count
        => Length;

    public bool IsEmpty
        => Length == 0;

    public bool Equals(LowerString other)
        => string.Equals(Lower, other.Lower, StringComparison.Ordinal);

    public bool Equals(string? other)
        => string.Equals(Lower, other, StringComparison.OrdinalIgnoreCase);

    public int CompareTo(LowerString other)
        => string.Compare(Lower, other.Lower, StringComparison.Ordinal);

    public int CompareTo(string? other)
        => string.Compare(Lower, other, StringComparison.OrdinalIgnoreCase);

    public bool Contains(LowerString other)
        => Lower.Contains(other.Lower, StringComparison.Ordinal);

    public bool Contains(string other)
        => Lower.Contains(other, StringComparison.OrdinalIgnoreCase);

    public bool StartsWith(LowerString other)
        => Lower.StartsWith(other.Lower, StringComparison.Ordinal);

    public bool StartsWith(string other)
        => Lower.StartsWith(other, StringComparison.OrdinalIgnoreCase);

    public bool EndsWith(LowerString other)
        => Lower.EndsWith(other.Lower, StringComparison.Ordinal);

    public bool EndsWith(string other)
        => Lower.EndsWith(other, StringComparison.OrdinalIgnoreCase);

    public bool IsContained(string other)
        => IsEmpty || other.Contains(Lower, StringComparison.OrdinalIgnoreCase);

    public override string ToString()
        => Text;

    public static implicit operator string(LowerString s)
        => s.Text;

    public static implicit operator LowerString(string s)
        => new(s);

    // Create an ImGui text input box with hint that converts the string to a LowerString.
    public static bool InputWithHint(string label, string hint, ref LowerString s, uint maxLength = 128,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        var tmp = s.Text;
        if (!ImGui.InputTextWithHint(label, hint, ref tmp, maxLength, flags) || tmp == s.Text)
            return false;

        s = new LowerString(tmp);
        return true;
    }

    public override bool Equals(object? obj)
        => obj is LowerString lowerString && Equals(lowerString);

    public override int GetHashCode()
        => Text.GetHashCode();

    public static bool operator ==(LowerString lhs, LowerString rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(LowerString lhs, LowerString rhs)
        => !lhs.Equals(rhs);

    public static bool operator ==(LowerString lhs, string rhs)
        => lhs.Equals(rhs);

    public static bool operator !=(LowerString lhs, string rhs)
        => !lhs.Equals(rhs);

    public static bool operator ==(string lhs, LowerString rhs)
        => rhs.Equals(lhs);

    public static bool operator !=(string lhs, LowerString rhs)
        => !rhs.Equals(lhs);

    private class Converter : JsonConverter<LowerString>
    {
        public override void WriteJson(JsonWriter writer, LowerString value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Text);
        }

        public override LowerString ReadJson(JsonReader reader, Type objectType, LowerString existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.Value is string text)
                return new LowerString(text);

            return existingValue;
        }
    }
}
