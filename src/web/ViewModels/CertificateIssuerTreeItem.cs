namespace Notary.Web.ViewModels
{
    public class CertificateIssuerTreeItem
    {
        public CertificateIssuerTreeItem()
        {
            Name = string.Empty;
            Slug = string.Empty;
        }

        public CertificateIssuerTreeItem(string name, string slug)
        {
            Name = name;
            Slug = slug;
        }

        public string Name { get; set; }

        public string Slug { get; set; }
    }
}
