using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Notary.Web.Shared;

public partial class MainLayout
{
    private bool _darkMode;
    private bool _drawerOpen = true;
    private MudThemeProvider? _themeProvider;

    [Inject] public NavigationManager NavigationManager { get; set; }

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
        NavigationManager.NavigateTo("Authenticate/Login",
            new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true });
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        _darkMode = newValue;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}