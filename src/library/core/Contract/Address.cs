using System.Runtime.Serialization;

namespace Notary.Contract
{
    [DataContract]
    public sealed class Address
    {
        /// <summary>
        /// Get or set the primary address line which is the address and street name
        /// </summary>
        [DataMember]
        public string AddressLine { get; set; }

        /// <summary>
        /// Get or set the city of residence
        /// </summary>
        [DataMember]
        public string City { get; set; }

        /// <summary>
        /// Get or set the country of residence
        /// </summary>
        [DataMember]
        public string Country { get; set; }

        /// <summary>
        /// Get or set the postal code
        /// </summary>
        [DataMember]
        public string PostalCode { get; set; }

        /// <summary>
        /// Get or set the second address line which is usually the unit number
        /// </summary>
        [DataMember]
        public string SecondAddressLine { get; set; }

        /// <summary>
        /// Get or set the state or province within the country of residence
        /// </summary>
        [DataMember]
        public string StateProvince { get; set; }

        /// <summary>
        /// Get or set the third address line which is usually an "attention" line.
        /// </summary>
        [DataMember]
        public string ThirdAddressLine { get; set; }
    }
}
