using System.Reflection;

namespace IINACT;

public static class PrivateHelpers
{
    public static T GetProperty<T>(this object obj, string propName)
    {
        var pi = obj.GetType()
                    .GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return (T)pi?.GetValue(obj, null)!;
    }

    public static T GetField<T>(this object obj, string propName)
    {
        var t = obj.GetType();
        FieldInfo? fi = null;
        while (fi == null && t != null)
        {
            fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }

        return (T)fi?.GetValue(obj)!;
    }

    public static void SetProperty<T>(this object obj, string propName, T val)
    {
        var t = obj.GetType();
        if (t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
        {
            throw new ArgumentOutOfRangeException(nameof(propName),
                                                  $@"Property {propName} was not found in Type {obj.GetType().FullName}");
        }

        t.InvokeMember(propName,
                       BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
                       null, obj,
                       new object?[] { val });
    }

    public static void SetField<T>(this object obj, string propName, T val)
    {
        var t = obj.GetType();
        FieldInfo? fi = null;
        while (fi == null && t != null)
        {
            fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }

        fi?.SetValue(obj, val);
    }

    public static MethodInfo? GetMethod(this object obj, string methodName)
    {
        var type = obj.GetType();
        var method = type.GetMethod(methodName);
        return method ?? type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public static object? CallMethod(this object obj, string methodName, object?[]? parameters)
    {
        var method = GetMethod(obj, methodName);
        return method?.Invoke(obj, parameters);
    }
}
