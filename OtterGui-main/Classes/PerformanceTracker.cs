using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Dalamud.Game;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Classes;

public class PerformanceTracker<T> : IDisposable where T : unmanaged, Enum
{
    private readonly Framework _framework;
    public           bool      Enabled     { get; private set; }
    public           long      TotalFrames { get; private set; }

    private readonly Monitor[] _monitors =
#if PROFILING
        Enum.GetValues<T>().Select(e => new Monitor()).Append(new Monitor()).ToArray();
#else
        Array.Empty<Monitor>();
#endif

    private unsafe struct Monitor
    {
        public const    int                    RollingFramesStored = 64;
        public const    uint                   RollingFramesMask   = RollingFramesStored - 1;
        public readonly ThreadLocal<Stopwatch> Stopwatch;
        public          ulong                  TotalTime;

        public        uint CaughtFrames;
        public        uint LastFrame;
        public        uint LongestFrame;
        public        uint AverageFrame;
        private fixed uint _lastFrames[RollingFramesStored];

        public Monitor()
            => Stopwatch = new ThreadLocal<Stopwatch>(() => new Stopwatch(), true);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Reset()
        {
            foreach (var watch in Stopwatch.Values)
                watch.Reset();

            LongestFrame = 0;
            TotalTime    = 0;
            LastFrame    = 0;
            AverageFrame = 0;
            CaughtFrames = 0;
            for (var i = 0; i < RollingFramesStored; ++i)
                _lastFrames[i] = 0;
        }


        public uint RollingAverageFrame
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            get
            {
                var sum = 0ul;
                for (var i = 0u; i < RollingFramesStored; ++i)
                {
                    if (_lastFrames[i] == 0)
                        return i == 0 ? 0 : (uint)(sum / i);

                    sum += _lastFrames[i];
                }

                return (uint)(sum / RollingFramesStored);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Update()
        {
            var mainThread = Stopwatch.Value!;

            LastFrame = (uint)mainThread.ElapsedTicks;
            if (LastFrame > 0)
            {
                if (LastFrame > LongestFrame)
                    LongestFrame = LastFrame;

                _lastFrames[CaughtFrames & RollingFramesMask] = LastFrame;

                AverageFrame = LastFrame + AverageFrame * CaughtFrames;
                ++CaughtFrames;
                AverageFrame /= CaughtFrames;
                TotalTime    += LastFrame;
            }

            mainThread.Reset();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private void OnFramework(Framework _)
    {
        foreach (ref var monitor in _monitors.AsSpan())
            monitor.Update();

        ++TotalFrames;
        _monitors.Last()!.Stopwatch.Value!.Start();
    }

    public PerformanceTracker(Framework framework)
        => _framework = framework;


    public readonly struct TimingStopper : IDisposable
    {
        private readonly Stopwatch _watch;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal TimingStopper(PerformanceTracker<T> manager, T type)
        {
            _watch = manager._monitors[Unsafe.As<T, int>(ref type)].Stopwatch.Value!;
            _watch.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void Dispose()
            => _watch.Stop();
    }

#if PROFILING
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public TimingStopper Measure(T timingType)
        => new(this, timingType);
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public IDisposable? Measure(T timingType)
        => null;
#endif

    [Conditional("PROFILING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Start(T timingType)
        => _monitors[Unsafe.As<T, int>(ref timingType)].Stopwatch.Value!.Start();

    [Conditional("PROFILING")]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Stop(T timingType)
        => _monitors[Unsafe.As<T, int>(ref timingType)].Stopwatch.Value!.Stop();

    [Conditional("PROFILING")]
    public void Enable()
    {
        if (Enabled)
            return;

        _framework.RunOnTick(() =>
        {
            _framework.Update += OnFramework;
            TotalFrames       =  0;
            foreach (ref var monitor in _monitors.AsSpan())
                monitor.Reset();

            Enabled = true;
        });
    }

    [Conditional("PROFILING")]
    public void Disable()
    {
        if (!Enabled)
            return;

        _framework.Update -= OnFramework;
        Enabled           =  false;
    }

    [Conditional("PROFILING")]
    public void Draw(string label, string textBox, Func<T, string> toNames)
    {
        using var id      = ImRaii.PushId(label);
        var       enabled = Enabled;
        if (ImGui.Checkbox(textBox, ref enabled))
        {
            if (enabled)
                Enable();
            else
                Disable();
        }

        if (enabled && TotalFrames > 0)
        {
            using var table = ImRaii.Table("##table", 8, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);

            void PrintTimeColumn(ulong frames)
            {
                ImGui.TableNextColumn();
                var value = frames * 1000.0 / Stopwatch.Frequency;
                var text = value switch
                {
                    > 3600000 => $"{(value / 3600000).ToString("F4", CultureInfo.InvariantCulture)} h",
                    > 60000   => $"{(value / 60000).ToString("F4", CultureInfo.InvariantCulture)} min",
                    > 1000    => $"{(value / 1000).ToString("F4", CultureInfo.InvariantCulture)} s",
                    _         => $"{value.ToString("F4", CultureInfo.InvariantCulture)} ms",
                };
                ImGuiUtil.RightAlign(text);
            }

            ImGui.TableSetupColumn("Name",            ImGuiTableColumnFlags.None, 200 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Last Frame",      ImGuiTableColumnFlags.None, 100 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Longest Frame",   ImGuiTableColumnFlags.None, 100 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Average Frame",   ImGuiTableColumnFlags.None, 100 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Rolling Average", ImGuiTableColumnFlags.None, 100 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("#Frames",         ImGuiTableColumnFlags.None, 75 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Total Time",      ImGuiTableColumnFlags.None, 100 * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("#T",              ImGuiTableColumnFlags.None, 25 * ImGuiHelpers.GlobalScale);
            ImGui.TableHeadersRow();

            var totalMonitor = _monitors.Last();
            ImGuiUtil.DrawTableColumn("Total Tracked Time");
            PrintTimeColumn(totalMonitor.LastFrame);
            PrintTimeColumn(totalMonitor.LongestFrame);
            PrintTimeColumn(totalMonitor.AverageFrame);
            PrintTimeColumn(totalMonitor.RollingAverageFrame);
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign(totalMonitor.CaughtFrames.ToString());
            PrintTimeColumn(totalMonitor.TotalTime);
            ImGui.TableNextColumn();
            ImGuiUtil.RightAlign("1");

            foreach (var (type, monitor) in Enum.GetValues<T>().Zip(_monitors))
            {
                ImGuiUtil.DrawTableColumn(toNames(type));
                PrintTimeColumn(monitor.LastFrame);
                PrintTimeColumn(monitor.LongestFrame);
                PrintTimeColumn(monitor.AverageFrame);
                PrintTimeColumn(monitor.RollingAverageFrame);
                ImGui.TableNextColumn();
                ImGuiUtil.RightAlign(monitor.CaughtFrames.ToString());
                var threadedTime = (ulong)monitor.Stopwatch.Values.Sum(m => m.ElapsedTicks) + monitor.TotalTime;
                PrintTimeColumn(threadedTime);
                ImGui.TableNextColumn();
                ImGuiUtil.RightAlign(monitor.Stopwatch.Values.Count.ToString());
            }
        }
    }

    public void Dispose()
    {
        Disable();
        foreach (ref var monitor in _monitors.AsSpan())
            monitor.Stopwatch.Dispose();
    }
}
