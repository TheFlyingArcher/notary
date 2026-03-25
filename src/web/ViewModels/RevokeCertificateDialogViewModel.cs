namespace Notary.Web.ViewModels;

public class RevokeCertificateDialogViewModel
{
    public string Slug { get; set; }

    public RevocationReason RevocationReason { get; set; }

    public string UserRevoking { get; set; }
}