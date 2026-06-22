using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Notary.Web.Shared;

public partial class MainLayout
{
    bool _drawerOpen = true;
    bool _darkMode = false;
    private MudThemeProvider? _themeProvider;

    public MainLayout()
    {
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _themeProvider != null)
        {
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
}