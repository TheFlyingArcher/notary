using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Notary;
using Notary.Contract;
using Notary.Interface.Service;
using Notary.Web.Shared;
using Notary.Web.ViewModels;

namespace Notary.Web.Pages;

[Authorize]
public partial class Index : ComponentBase
{
    protected DateTime Now { get; } = DateTime.UtcNow;

    protected List<Certificate> AllCertificates { get; } = new();

    protected List<Certificate> ValidCertificates
    {
        get
        {
            return AllCertificates.Where(a => a.NotAfter.AddDays(-30) > Now).ToList();
        }
    }

    protected List<Certificate> ExpiringCertificates
    {
        get
        {
            return AllCertificates.Where(a => Now > a.NotAfter.AddDays(-30) && Now <= a.NotAfter).ToList();
        }
    }

    protected List<Certificate> ExpiredCertificates
    {
        get
        {
            return AllCertificates.Where(a => Now > a.NotAfter).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var certificates = await CertificateService.GetAllAsync();
        AllCertificates.AddRange(certificates);
    }

    [Inject]
    public ICertificateService CertificateService { get; set; }
}