using System.Diagnostics;
using System.Reflection;
using Microsoft.Toolkit.Diagnostics;

namespace IINACT;

public static class PrivateHelpers {
    public static T GetProperty<T>(this object obj, string propName) {
        Guard.IsNotNull(obj, nameof(obj));
        var pi = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Guard.IsNotNull(pi, nameof(pi));
        return (T)pi.GetValue(obj, null)!;
    }

    public static T GetField<T>(this object obj, string propName) {
        Guard.IsNotNull(obj, nameof(obj));
        var t = obj.GetType();
        FieldInfo? fi = null;
        while (fi == null && t != null) {
            fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }
        Guard.IsNotNull(fi, nameof(fi));
        return (T)fi.GetValue(obj)!;
    }

    public static void SetProperty<T>(this object obj, string propName, T val) {
        var t = obj.GetType();
        if (t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            throw new ArgumentOutOfRangeException(nameof(propName),
                $@"Property {propName} was not found in Type {obj.GetType().FullName}");
        t.InvokeMember(propName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj,
            new object?[] { val });
    }

    public static void SetField<T>(this object obj, string propName, T val) {
        Guard.IsNotNull(obj, nameof(obj));
        var t = obj.GetType();
        FieldInfo? fi = null;
        while (fi == null && t != null) {
            fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }
        Guard.IsNotNull(fi, nameof(fi));
        fi.SetValue(obj, val);
    }

    public static MethodInfo? GetMethod(this object obj, string methodName) {
        Guard.IsNotNull(obj, nameof(obj));
        return obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public static object? CallMethod(this object obj, string methodName, object?[]? parameters) {
        var method = GetMethod(obj, methodName);
        Guard.IsNotNull(method, nameof(method));
        return method.Invoke(obj, parameters);
    }
}