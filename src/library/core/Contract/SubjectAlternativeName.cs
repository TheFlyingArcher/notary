namespace Notary.Contract
{
    public class SubjectAlternativeName
    {
        /// <summary>
        /// The kind of SAN
        /// </summary>
        public SanKind Kind
        {
            get; set;
        }

        /// <summary>
        /// Subject Alternative Name. Can be a DNS or e-mail
        /// </summary>
        public string Name
        {
            get; set;
        }
    }
}