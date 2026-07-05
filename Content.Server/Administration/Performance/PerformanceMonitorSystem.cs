using System.Diagnostics;
using Content.Shared.Administration;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Administration.Performance;

/// <summary>
/// Collects lightweight server performance metrics for the <c>serverperf</c> admin panel.
/// </summary>
/// <remarks>
/// Per tick this only reads the engine clock and writes into a fixed accumulator; everything
/// else (process CPU/memory, GC, net stats) is sampled once per second into a ring buffer,
/// so the monitor itself has no measurable cost.
/// </remarks>
public sealed partial class PerformanceMonitorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>Seconds of per-second history kept (5 minutes).</summary>
    public const int HistorySize = 300;

    /// <summary>Seconds of history sent to the panel (2 minutes).</summary>
    public const int HistorySent = 120;

    /// <summary>A tick counts as late when its spacing exceeds budget by this factor.</summary>
    public const float LateFactor = 1.25f;

    /// <summary>Raised once per second after a new sample lands, so open EUIs can push state.</summary>
    public event Action? OnSample;

    // Exported through the engine's Prometheus endpoint (metrics.enabled). Same numbers the
    // serverperf panel shows; process CPU/memory/GC are already covered by prometheus-net's
    // built-in collectors and the engine's robust_server_update_usage histograms.
    private static readonly Gauge TicksPerSecondMetric = Metrics.CreateGauge(
        "iss14_ticks_per_second", "Server ticks processed in the last second.");
    private static readonly Gauge TickSpacingAvgMetric = Metrics.CreateGauge(
        "iss14_tick_spacing_avg_ms", "Average wall-clock spacing between server ticks over the last second (ms).");
    private static readonly Gauge TickSpacingMaxMetric = Metrics.CreateGauge(
        "iss14_tick_spacing_max_ms", "Worst wall-clock spacing between server ticks over the last second (ms).");
    private static readonly Gauge LateTicksMetric = Metrics.CreateGauge(
        "iss14_late_ticks", "Ticks in the last second that ran more than 25% over the tick budget.");

    private readonly PerfSample[] _samples = new PerfSample[HistorySize];
    private int _sampleCount;
    private int _sampleHead;

    private readonly Process _process = Process.GetCurrentProcess();
    private readonly System.Diagnostics.Stopwatch _uptime = System.Diagnostics.Stopwatch.StartNew();

    // Current-second accumulator.
    private TimeSpan _lastTick = TimeSpan.MinValue;
    private TimeSpan _windowStart;
    private int _windowTicks;
    private double _windowSumMs;
    private double _windowMaxMs;
    private int _windowLate;

    // Previous-sample baselines for delta rates.
    private TimeSpan _lastCpuTime;
    private long _lastNetSent;
    private long _lastNetRecv;
    private int _lastGc0;
    private int _lastGc1;
    private int _lastGc2;

    public override void Update(float frameTime)
    {
        var now = _timing.RealTime;

        if (_lastTick == TimeSpan.MinValue)
        {
            _lastTick = now;
            _windowStart = now;
            _lastCpuTime = _process.TotalProcessorTime;
            var stats = _net.Statistics;
            _lastNetSent = stats.SentBytes;
            _lastNetRecv = stats.ReceivedBytes;
            _lastGc0 = GC.CollectionCount(0);
            _lastGc1 = GC.CollectionCount(1);
            _lastGc2 = GC.CollectionCount(2);
            return;
        }

        var deltaMs = (now - _lastTick).TotalMilliseconds;
        _lastTick = now;

        _windowTicks += 1;
        _windowSumMs += deltaMs;
        if (deltaMs > _windowMaxMs)
            _windowMaxMs = deltaMs;

        var budgetMs = 1000.0 / _timing.TickRate;
        if (deltaMs > budgetMs * LateFactor)
            _windowLate += 1;

        if ((now - _windowStart).TotalSeconds >= 1)
            TakeSample(now);
    }

    private void TakeSample(TimeSpan now)
    {
        var wallSeconds = (now - _windowStart).TotalSeconds;

        _process.Refresh();
        var cpuTime = _process.TotalProcessorTime;
        var cpuCores = (float) ((cpuTime - _lastCpuTime).TotalSeconds / wallSeconds);
        _lastCpuTime = cpuTime;

        var stats = _net.Statistics;
        var sentPerSec = (long) ((stats.SentBytes - _lastNetSent) / wallSeconds);
        var recvPerSec = (long) ((stats.ReceivedBytes - _lastNetRecv) / wallSeconds);
        _lastNetSent = stats.SentBytes;
        _lastNetRecv = stats.ReceivedBytes;

        var gc0 = GC.CollectionCount(0);
        var gc1 = GC.CollectionCount(1);
        var gc2 = GC.CollectionCount(2);

        var sample = new PerfSample(
            AvgTickMs: _windowTicks > 0 ? (float) (_windowSumMs / _windowTicks) : 0f,
            MaxTickMs: (float) _windowMaxMs,
            Ticks: _windowTicks,
            LateTicks: _windowLate,
            CpuCores: cpuCores,
            WorkingSet: _process.WorkingSet64,
            ManagedHeap: GC.GetTotalMemory(false),
            Gc0: gc0 - _lastGc0,
            Gc1: gc1 - _lastGc1,
            Gc2: gc2 - _lastGc2,
            NetSentPerSec: sentPerSec,
            NetRecvPerSec: recvPerSec);

        _lastGc0 = gc0;
        _lastGc1 = gc1;
        _lastGc2 = gc2;

        _samples[_sampleHead] = sample;
        _sampleHead = (_sampleHead + 1) % HistorySize;
        if (_sampleCount < HistorySize)
            _sampleCount += 1;

        TicksPerSecondMetric.Set(sample.Ticks);
        TickSpacingAvgMetric.Set(sample.AvgTickMs);
        TickSpacingMaxMetric.Set(sample.MaxTickMs);
        LateTicksMetric.Set(sample.LateTicks);

        _windowStart = now;
        _windowTicks = 0;
        _windowSumMs = 0;
        _windowMaxMs = 0;
        _windowLate = 0;

        OnSample?.Invoke();
    }

    public ServerPerfEuiState GetState()
    {
        var state = new ServerPerfEuiState
        {
            TargetTickRate = (int) _timing.TickRate,
            TickBudgetMs = 1000f / _timing.TickRate,
            ProcessorCount = Environment.ProcessorCount,
            EntityCount = EntityManager.EntityCount,
            PlayerCount = _players.PlayerCount,
            GcPausePercent = (float) GC.GetGCMemoryInfo().PauseTimePercentage,
            Uptime = _uptime.Elapsed,
        };

        if (_sampleCount == 0)
            return state;

        var newest = Sample(1);
        state.ActualTps = newest.Ticks;
        state.AvgTickMs = newest.AvgTickMs;
        state.MaxTickMs = newest.MaxTickMs;
        state.CpuCoresUsed = newest.CpuCores;
        state.WorkingSetBytes = newest.WorkingSet;
        state.ManagedHeapBytes = newest.ManagedHeap;
        state.NetSentBytesPerSec = newest.NetSentPerSec;
        state.NetRecvBytesPerSec = newest.NetRecvPerSec;

        var minute = Math.Min(60, _sampleCount);
        for (var i = 1; i <= minute; i++)
        {
            var s = Sample(i);
            state.LateTicksLastMinute += s.LateTicks;
            state.Gc0LastMinute += s.Gc0;
            state.Gc1LastMinute += s.Gc1;
            state.Gc2LastMinute += s.Gc2;
        }

        var histLen = Math.Min(HistorySent, _sampleCount);
        state.HistoryAvgTickMs = new float[histLen];
        state.HistoryMaxTickMs = new float[histLen];
        state.HistoryCpuCores = new float[histLen];
        for (var i = 0; i < histLen; i++)
        {
            // Oldest first: index histLen back from the head.
            var s = Sample(histLen - i);
            state.HistoryAvgTickMs[i] = s.AvgTickMs;
            state.HistoryMaxTickMs[i] = s.MaxTickMs;
            state.HistoryCpuCores[i] = s.CpuCores;
        }

        return state;
    }

    /// <summary>Gets the sample <paramref name="age"/> steps back from the head (1 = newest).</summary>
    private PerfSample Sample(int age)
    {
        return _samples[(_sampleHead - age + HistorySize) % HistorySize];
    }

    private readonly record struct PerfSample(
        float AvgTickMs,
        float MaxTickMs,
        int Ticks,
        int LateTicks,
        float CpuCores,
        long WorkingSet,
        long ManagedHeap,
        int Gc0,
        int Gc1,
        int Gc2,
        long NetSentPerSec,
        long NetRecvPerSec);
}
