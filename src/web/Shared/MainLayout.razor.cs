using Microsoft.AspNetCore.Components;
using MudBlazor;
using Notary.Loading;

namespace Notary.Web.Shared;

public partial class MainLayout : IDisposable
{
    bool _drawerOpen = true;
    bool _darkMode = false;
    bool _loading = false;
    private MudThemeProvider? _themeProvider;

    public MainLayout()
    {
    }

    protected override void OnInitialized()
    {
        _loading = LoadingTracker.IsLoading;
        LoadingTracker.Changed += OnLoadingChanged;
    }

    public void Dispose()
    {
        LoadingTracker.Changed -= OnLoadingChanged;
    }

    // The tracker raises Changed from a timer thread, so hop onto the renderer's context before touching the UI
    private void OnLoadingChanged()
    {
        try
        {
            _ = InvokeAsync(() =>
            {
                _loading = LoadingTracker.IsLoading;
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // The circuit ended while the event was in flight
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _themeProvider != null)
        {
            _darkMode = await _themeProvider.GetSystemDarkModeAsync();
            await _themeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);
            StateHasChanged();
        }
    }

    private void OnLoginClick()
    {
        NavigationManager.NavigateTo("Authenticate/Login", new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true });
    }

    private Task OnSystemDarkModeChanged(bool newValue)
    {
        _darkMode = newValue;
        StateHasChanged();
        return Task.CompletedTask;
    }

    void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    public ILoadingTracker LoadingTracker { get; set; } = default!;
}