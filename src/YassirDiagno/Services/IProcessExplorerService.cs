using System.Diagnostics;

namespace YassirDiagno.Services;

public sealed class ProcessInfo
{
    public required int Pid { get; init; }
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required double MemoryMB { get; init; }
    public required double CpuPercent { get; init; }
    public required int Threads { get; init; }
    public required string StartTime { get; init; }
}

public interface IProcessExplorerService
{
    Task<List<ProcessInfo>> GetTopProcessesAsync(int count = 20, string sortBy = "cpu");
    bool TryKill(int pid);
}

public sealed class ProcessExplorerService : IProcessExplorerService
{
    private readonly Dictionary<int, (DateTime time, TimeSpan total)> _lastCpuSample = new();

    public async Task<List<ProcessInfo>> GetTopProcessesAsync(int count = 20, string sortBy = "cpu")
    {
        return await Task.Run(() =>
        {
            var procs = Process.GetProcesses();
            var now = DateTime.UtcNow;
            var coreCount = Environment.ProcessorCount;
            var infos = new List<ProcessInfo>();

            foreach (var p in procs)
            {
                try
                {
                    var pid = p.Id;
                    if (pid == 0) continue;

                    double cpuPct = 0;
                    var currentCpu = p.TotalProcessorTime;
                    if (_lastCpuSample.TryGetValue(pid, out var prev))
                    {
                        var dt = (now - prev.time).TotalSeconds;
                        if (dt > 0.1)
                        {
                            cpuPct = (currentCpu - prev.total).TotalSeconds / dt / coreCount * 100;
                            cpuPct = Math.Max(0, Math.Min(100, cpuPct));
                        }
                    }
                    _lastCpuSample[pid] = (now, currentCpu);

                    string title = "";
                    try { title = p.MainWindowTitle ?? ""; } catch { }
                    string start = "";
                    try { start = p.StartTime.ToString("HH:mm:ss"); } catch { }

                    infos.Add(new ProcessInfo
                    {
                        Pid = pid,
                        Name = p.ProcessName,
                        Title = title,
                        MemoryMB = Math.Round(p.WorkingSet64 / 1024.0 / 1024.0, 1),
                        CpuPercent = Math.Round(cpuPct, 1),
                        Threads = p.Threads.Count,
                        StartTime = start
                    });
                }
                catch { }
                finally { p.Dispose(); }
            }

            IOrderedEnumerable<ProcessInfo> sorted = sortBy.ToLowerInvariant() switch
            {
                "ram" or "memory" => infos.OrderByDescending(x => x.MemoryMB),
                "name"            => infos.OrderBy(x => x.Name),
                "pid"             => infos.OrderBy(x => x.Pid),
                _                 => infos.OrderByDescending(x => x.CpuPercent).ThenByDescending(x => x.MemoryMB),
            };
            return sorted.Take(count).ToList();
        });
    }

    public bool TryKill(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            p.Kill();
            return true;
        }
        catch { return false; }
    }
}
