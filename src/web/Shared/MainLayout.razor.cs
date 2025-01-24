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
            _darkMode = await _themeProvider.GetSystemPreference();
            await _themeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            StateHasChanged();
        }
    }

    private void OnLoginClick()
    {
        NavigationManager.NavigateTo("Authenticate/Login", new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true });
    }

    private Task OnSystemPreferenceChanged(bool newValue)
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