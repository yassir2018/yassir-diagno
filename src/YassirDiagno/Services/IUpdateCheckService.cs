using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace YassirDiagno.Services;

public sealed record UpdateInfo(string CurrentVersion, string LatestVersion, bool IsUpdateAvailable, string ReleaseUrl, string ReleaseNotes);

public interface IUpdateCheckService
{
    Task<UpdateInfo?> CheckAsync(CancellationToken ct = default);
    string CurrentVersion { get; }
}

public sealed class UpdateCheckService : IUpdateCheckService
{
    private const string ReleasesApi = "https://api.github.com/repos/yassir2018/yassir-diagno/releases/latest";
    private readonly HttpClient _http;

    public string CurrentVersion { get; }

    public UpdateCheckService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        _http.DefaultRequestHeaders.Add("User-Agent", "YassirDiagno-UpdateCheck/1.0");
        _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = version is not null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
    }

    public async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync(ReleasesApi, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var latestTag = root.GetProperty("tag_name").GetString() ?? "";
            var url = root.GetProperty("html_url").GetString() ?? "";
            var body = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "";

            var current = CurrentVersion.TrimStart('v');
            var latest = latestTag.TrimStart('v');
            var isNewer = CompareVersions(latest, current) > 0;

            return new UpdateInfo(CurrentVersion, latestTag, isNewer, url, body);
        }
        catch
        {
            return null;
        }
    }

    private static int CompareVersions(string a, string b)
    {
        var aParts = a.Split('.', '-').Take(3).Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var bParts = b.Split('.', '-').Take(3).Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        for (int i = 0; i < 3; i++)
        {
            var av = i < aParts.Length ? aParts[i] : 0;
            var bv = i < bParts.Length ? bParts[i] : 0;
            if (av != bv) return av.CompareTo(bv);
        }
        return 0;
    }
}
