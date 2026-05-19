using System.Windows;

namespace YassirDiagno.Services;

public enum AppTheme { Light, Dark }

public interface IThemeService
{
    AppTheme Current { get; }
    event EventHandler<AppTheme>? ThemeChanged;
    void Apply(AppTheme theme);
    void Toggle();
}

public sealed class ThemeService : IThemeService
{
    private readonly ISettingsService _settings;
    public AppTheme Current { get; private set; } = AppTheme.Light;
    public event EventHandler<AppTheme>? ThemeChanged;

    private const string LightThemeUri = "Resources/Themes/LightTheme.xaml";
    private const string DarkThemeUri = "Resources/Themes/DarkTheme.xaml";

    public ThemeService(ISettingsService settings)
    {
        _settings = settings;
        var initial = string.Equals(_settings.Current.Theme, "Dark", StringComparison.OrdinalIgnoreCase)
            ? AppTheme.Dark : AppTheme.Light;
        Apply(initial);
    }

    public void Apply(AppTheme theme)
    {
        var uri = theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri;
        var dict = new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) };

        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            var src = merged[i].Source?.OriginalString ?? "";
            if (src.Contains("LightTheme.xaml", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase))
            {
                merged.RemoveAt(i);
            }
        }
        merged.Insert(0, dict);

        Current = theme;
        _settings.Current.Theme = theme.ToString();
        _settings.Save();
        ThemeChanged?.Invoke(this, theme);
    }

    public void Toggle() => Apply(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
}
