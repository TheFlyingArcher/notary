using System;
using System.Collections.Generic;
using System.Text;

namespace Notary
{
    public enum CertificateFormat
    {
        Der = 1,
        Pkcs7 = 2,
        Pkcs12 = 4,
        Pem = 8,
        Pkcs7Pem = 16
    }
}