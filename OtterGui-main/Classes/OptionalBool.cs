using System;
using System.Reflection.Metadata.Ecma335;
using Lumina.Data.Parsing;
using Newtonsoft.Json.Linq;

namespace OtterGui.Classes;

public readonly struct OptionalBool : IEquatable<OptionalBool>, IEquatable<bool?>, IEquatable<bool>
{
    private readonly byte _value;

    public static readonly OptionalBool True  = new(true);
    public static readonly OptionalBool False = new(false);
    public static readonly OptionalBool Null  = new();

    public OptionalBool()
        => _value = byte.MaxValue;

    public OptionalBool(bool? value)
        => _value = (byte)(value == null ? byte.MaxValue : value.Value ? 1 : 0);

    public bool HasValue
        => _value < 2;

    public bool? Value
        => _value switch
        {
            1 => true,
            0 => false,
            _ => null,
        };

    public static implicit operator OptionalBool(bool? v)
        => new(v);

    public static implicit operator OptionalBool(bool v)
        => new(v);

    public static implicit operator bool?(OptionalBool v)
        => v.Value;

    public bool Equals(OptionalBool other)
        => _value == other._value;

    public bool Equals(bool? other)
        => _value switch
        {
            1 when other != null => other.Value,
            0 when other != null => !other.Value,
            _ when other == null => true,
            _                    => false,
        };

    public bool Equals(bool other)
        => other ? _value == 1 : _value == 0;

    public override string ToString()
        => _value switch
        {
            1 => true.ToString(),
            0 => false.ToString(),
            _ => "null",
        };
}

public readonly struct QuadBool : IEquatable<QuadBool>, IEquatable<OptionalBool>, IEquatable<bool?>, IEquatable<bool>
{
    private readonly       byte     _value;
    public static readonly QuadBool True      = new(true, true);
    public static readonly QuadBool False     = new(false, true);
    public static readonly QuadBool NullTrue  = new(true, false);
    public static readonly QuadBool NullFalse = new(false, false);
    public static readonly QuadBool Null      = NullTrue;

    public QuadBool(bool state, bool enabled)
    {
        _value = (state, enabled) switch
        {
            (true, true)   => 1,
            (true, false)  => 0,
            (false, true)  => 3,
            (false, false) => 2,
        };
    }

    public QuadBool()
        : this(false, false)
    { }

    public QuadBool(bool? b)
    {
        _value = b switch
        {
            null  => 3,
            true  => 1,
            false => 0,
        };
    }

    public QuadBool(bool b)
        => _value = (byte)(b ? 1 : 0);

    public QuadBool(OptionalBool b)
        : this(b.Value)
    { }

    public static implicit operator QuadBool(bool? v)
        => new(v);

    public static implicit operator QuadBool(bool v)
        => new(v);

    public static implicit operator bool?(QuadBool v)
        => v.Value;

    public static implicit operator OptionalBool(QuadBool v)
        => v._value switch
        {
            0 => OptionalBool.False,
            1 => OptionalBool.True,
            _ => OptionalBool.Null,
        };

    public (bool Value, bool Enabled) Split
        => (ForcedValue, Enabled);

    public bool? Value
        => _value switch
        {
            0 => false,
            1 => true,
            _ => (bool?)null,
        };

    public bool Enabled
        => _value > 2;

    public bool ForcedValue
        => (_value & 1) == 1;

    public QuadBool SetEnabled(bool state)
        => new(ForcedValue, state);

    public QuadBool SetValue(bool value)
        => new(value, Enabled);

    public bool Equals(QuadBool other)
        => _value == other._value;

    public bool Equals(OptionalBool other)
        => other.Value == Value;

    public bool Equals(bool? other)
        => other == Value;

    public bool Equals(bool other)
        => other == Value;

    public override bool Equals(object? obj)
        => obj is QuadBool other && Equals(other);

    public override int GetHashCode()
        => _value;

    public override string ToString()
        => _value switch
        {
            1 => true.ToString(),
            0 => false.ToString(),
            3 => "null_true",
            _ => "null_false",
        };

    public bool TryParse(string text, out QuadBool b)
    {
        (var ret, b) = text.ToLowerInvariant() switch
        {
            "true"       => (true, True),
            "false"      => (true, False),
            "null"       => (true, Null),
            "null_true"  => (true, NullTrue),
            "null_false" => (true, NullFalse),
            _            => (false, Null),
        };
        return ret;
    }

    public JObject ToJObject(string nameValue, string nameEnabled)
        => new()
        {
            [nameValue]   = ForcedValue,
            [nameEnabled] = Enabled,
        };

    public static QuadBool FromJObject(JToken? token, string nameValue, string nameEnabled, QuadBool def)
    {
        if (token == null)
            return def;

        var value   = token[nameValue]?.ToObject<bool>() ?? def.ForcedValue;
        var enabled = token[nameEnabled]?.ToObject<bool>() ?? def.Enabled;
        return new QuadBool(value, enabled);
    }

    public static bool operator ==(QuadBool left, QuadBool right)
        => left.Equals(right);
    public static bool operator !=(QuadBool left, QuadBool right)
        => left.Equals(right);
}
