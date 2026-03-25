namespace Notary.Configuration;

public class NotaryActiveDirectoryConfiguration
{
    public string AdminGroupName { get; set; }

    public string CertificateAdminGroupName { get; set; }

    public string Domain { get; set; }

    public string SearchBase { get; set; }

    public string ServerName { get; set; }

    public string ServiceAccountPassword { get; set; }

    public string ServiceAccountUser { get; set; }
}