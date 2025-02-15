using System;
using AutoMapper;
using Notary.Contract;
using Notary.Data.Model;

namespace Notary.Data;

/// <summary>
/// A profile to map database models to over-the-wire contracts
/// </summary>
public class ModelMapProfile : Profile
{
    /// <summary>
    /// Instantiate a new model mapping profile
    /// </summary>
    public ModelMapProfile()
    {
        // Sub types
        CreateMap<Address, AddressModel>().ReverseMap();
        CreateMap<DistinguishedName, DistinguishedNameModel>().ReverseMap();
        CreateMap<SubjectAlternativeName, SanModel>().ReverseMap();

        // Core models
        CreateMap<AsymmetricKey, AsymmetricKeyModel>().ReverseMap();
        CreateMap<Certificate, CertificateModel>().ReverseMap();
        CreateMap<CertificateAuthority, CertificateAuthorityModel>().ReverseMap();
        CreateMap<RevocatedCertificate, RevocatedCertificateModel>().ReverseMap();
    }
}
