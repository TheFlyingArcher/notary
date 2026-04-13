using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Notary.Configuration;
using Notary.Interface.Repository;
using Notary.Interface.Service;

namespace Notary.Web.Shared;

public partial class MainLayout
{
    private bool _darkMode;
    private bool _drawerOpen = true;
    private bool _invalidLogin = false;
    private MudThemeProvider? _themeProvider;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _persist = false;

    [Inject] public NavigationManager NavigationManager { get; set; }

    [Inject] public NotaryConfiguration NotaryConfiguration { get; set; }
    
    [Inject] public IAccountService AccountService { get; set; }
    
    [Inject] public ITokenService TokenService { get; set; }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _themeProvider != null)
        {
            _darkMode = await _themeProvider.GetSystemPreference();
            await _themeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            StateHasChanged();
        }
    }

    private async Task OnLoginClick()
    {
        switch (NotaryConfiguration.Authentication)
        {
            case AuthenticationProvider.ActiveDirectory:
            case AuthenticationProvider.System:
                var account = await AccountService.AuthenticateAsync(_username, _password);
                if (account == null)
                {
                    _invalidLogin = true;
                }
                else
                {
                    var token = await TokenService.CreateToken(account);
                }
                break;
            case AuthenticationProvider.OpenId:
                NavigationManager.NavigateTo("Authenticate/Login",
                    new NavigationOptions { ForceLoad = true, ReplaceHistoryEntry = true });
                break;
            default:
                throw new NotaryException("Authentication is not supported.");
        }
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