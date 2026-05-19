namespace YassirDiagno.Services;

public interface INavigationService
{
    event EventHandler<NavigationEventArgs>? Navigated;
    void NavigateTo(string viewKey);
    string? CurrentView { get; }
}

public sealed class NavigationEventArgs : EventArgs
{
    public required string ViewKey { get; init; }
}

public sealed class NavigationService : INavigationService
{
    public string? CurrentView { get; private set; }
    public event EventHandler<NavigationEventArgs>? Navigated;

    public void NavigateTo(string viewKey)
    {
        if (CurrentView == viewKey) return;
        CurrentView = viewKey;
        Navigated?.Invoke(this, new NavigationEventArgs { ViewKey = viewKey });
    }
}
