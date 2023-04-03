using System;
using ImGuiNET;

namespace OtterGui.Raii;

// Push an arbitrary amount of ids into an object that are all popped when it is disposed.
// If condition is false, no id is pushed.
public static partial class ImRaii
{
    public static Id PushId(string id, bool enabled = true)
        => enabled ? new Id().Push(id) : new Id();

    public static Id PushId(int id, bool enabled = true)
        => enabled ? new Id().Push(id) : new Id();

    public static Id PushId(IntPtr id, bool enabled = true)
        => enabled ? new Id().Push(id) : new Id();

    public sealed class Id : IDisposable
    {
        private int _count;

        public Id Push(string id, bool condition = true)
        {
            if (condition)
            {
                ImGui.PushID(id);
                ++_count;
            }

            return this;
        }

        public Id Push(int id, bool condition = true)
        {
            if (condition)
            {
                ImGui.PushID(id);
                ++_count;
            }

            return this;
        }

        public Id Push(IntPtr id, bool condition = true)
        {
            if (condition)
            {
                ImGui.PushID(id);
                ++_count;
            }

            return this;
        }

        public void Pop(int num = 1)
        {
            num    =  Math.Min(num, _count);
            _count -= num;
            while (num-- > 0)
                ImGui.PopID();
        }

        public void Dispose()
            => Pop(_count);
    }
}
