using System.Runtime.InteropServices;

namespace IINACT.Network;

public static class SpanExtensions
{
    public static U Cast<T, U>(this Span<T> input) where T : struct where U : struct
    {
        return MemoryMarshal.Cast<T, U>(input)[0];
    }
    
    public static U Cast<T, U>(this ReadOnlySpan<T> input) where T : struct where U : struct
    {
        return MemoryMarshal.Cast<T, U>(input)[0];
    }
    
    public static T CastTo<T>(this Span<byte> input) where T : struct
    {
        return MemoryMarshal.Cast<byte, T>(input)[0];
    }
    
    public static T CastTo<T>(this ReadOnlySpan<byte> input) where T : struct
    {
        return MemoryMarshal.Cast<byte, T>(input)[0];
    }
}
