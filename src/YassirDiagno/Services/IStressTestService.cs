namespace YassirDiagno.Services;

public sealed class StressTestProgress
{
    public int ElapsedSeconds { get; init; }
    public int TotalSeconds { get; init; }
    public double CurrentCpuTemp { get; init; }
    public double PeakCpuTemp { get; init; }
    public bool IsRunning { get; init; }
}

public interface IStressTestService
{
    bool IsRunning { get; }
    event EventHandler<StressTestProgress>? ProgressChanged;
    Task RunCpuStressAsync(int durationSeconds, CancellationToken ct = default);
    void Stop();
}

public sealed class StressTestService : IStressTestService
{
    private readonly Services.Hardware.IHardwareMonitorService _monitor;
    private CancellationTokenSource? _cts;
    public bool IsRunning { get; private set; }
    public event EventHandler<StressTestProgress>? ProgressChanged;

    public StressTestService(Services.Hardware.IHardwareMonitorService monitor) => _monitor = monitor;

    public async Task RunCpuStressAsync(int durationSeconds, CancellationToken ct = default)
    {
        if (IsRunning) return;
        IsRunning = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;

        var threadCount = Environment.ProcessorCount;
        var threads = new List<Thread>();
        for (int i = 0; i < threadCount; i++)
        {
            var t = new Thread(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var x = 0.0;
                    for (int j = 0; j < 100000 && !token.IsCancellationRequested; j++)
                        x += Math.Sqrt(j) * Math.Sin(j) / (Math.Cos(j) + 1.1);
                }
            }) { IsBackground = true, Priority = ThreadPriority.Normal };
            t.Start();
            threads.Add(t);
        }

        double peak = 0;
        var start = DateTime.Now;
        try
        {
            while (!token.IsCancellationRequested)
            {
                var elapsed = (int)(DateTime.Now - start).TotalSeconds;
                if (elapsed >= durationSeconds) break;

                var snap = _monitor.ReadSnapshot();
                var current = snap.CpuSilicon ?? 0;
                if (current > peak) peak = current;

                ProgressChanged?.Invoke(this, new StressTestProgress
                {
                    ElapsedSeconds = elapsed,
                    TotalSeconds = durationSeconds,
                    CurrentCpuTemp = current,
                    PeakCpuTemp = peak,
                    IsRunning = true
                });
                await Task.Delay(500, token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _cts.Cancel();
            foreach (var t in threads) t.Join(1000);
            IsRunning = false;
            ProgressChanged?.Invoke(this, new StressTestProgress
            {
                ElapsedSeconds = durationSeconds,
                TotalSeconds = durationSeconds,
                CurrentCpuTemp = _monitor.ReadSnapshot().CpuSilicon ?? 0,
                PeakCpuTemp = peak,
                IsRunning = false
            });
        }
    }

    public void Stop() => _cts?.Cancel();
}
