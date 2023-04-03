using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Classes;

public class StartTimeTracker<T> where T : unmanaged, Enum
{
    private readonly DateTime                     _constructionTime = DateTime.UtcNow;
    private readonly (Stopwatch, TimeSpan, int)[] _timers = Enum.GetValues<T>().Select(e => (new Stopwatch(), TimeSpan.Zero, 0)).ToArray();

    public readonly struct TimingStopper : IDisposable
    {
        private readonly Stopwatch _watch;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TimingStopper(StartTimeTracker<T> manager, T type)
        {
            ref var tuple = ref manager._timers[Unsafe.As<T, int>(ref type)];
            _watch = tuple.Item1;
            _watch.Start();
            tuple.Item2 = DateTime.UtcNow - manager._constructionTime;
            tuple.Item3 = Thread.CurrentThread.ManagedThreadId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => _watch.Stop();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TimingStopper Measure(T timingType)
        => new(this, timingType);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Measure(T timingType, Action action)
    {
        using var t = Measure(timingType);
        action();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TRet Measure<TRet>(T timingType, Func<TRet> func)
    {
        using var t = Measure(timingType);
        return func();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Start(T timingType)
    {
        ref var pair = ref _timers[Unsafe.As<T, int>(ref timingType)];
        pair.Item1.Start();
        pair.Item2 = DateTime.UtcNow - _constructionTime;
        pair.Item3 = Thread.CurrentThread.ManagedThreadId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Stop(T timingType)
        => _timers[Unsafe.As<T, int>(ref timingType)].Item1.Stop();

    public void Draw(string label, Func<T, string> toNames)
    {
        using var id    = ImRaii.PushId(label);
        using var table = ImRaii.Table("##startTimeTable", 5, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);

        ImGui.TableSetupColumn("Name",   ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Time",   ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Start",  ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("End",    ImGuiTableColumnFlags.None, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Thread", ImGuiTableColumnFlags.None, 50 * ImGuiHelpers.GlobalScale);
        ImGui.TableHeadersRow();

        foreach (var (e, (timer, startTime, thread)) in Enum.GetValues<T>().Zip(_timers))
        {
            ImGuiUtil.DrawTableColumn(toNames(e));
            var time = timer.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(time.ToString("F4", CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(startTime.TotalMilliseconds.ToString("F4", CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign((time + startTime.TotalMilliseconds).ToString("F4", CultureInfo.InvariantCulture));
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(thread.ToString());
        }
    }
}
