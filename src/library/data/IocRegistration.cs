using System;
using Autofac;
using AutoMapper;
using MongoDB.Driver;

using Notary.Configuration;
using Notary.Data.Repository;
using Notary.Interface.Repository;
using Notary.IOC;

namespace Notary.Data
{
    public class IocRegistration : Module, IRegister
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register the model mappings
            builder.Register(r =>
            {
                var cfg = new MapperConfiguration(c =>
                {
                    c.AddProfile<ModelMapProfile>();
                });

                return cfg.CreateMapper();
            }).As<IMapper>().SingleInstance();

            builder.Register(r =>
            {
                var config = r.Resolve<NotaryConfiguration>();

                //NOTE: This might not work with MongoDB Atlas. Investigate
                var settings = MongoClientSettings.FromUrl(new MongoUrl(config.Database.Host));
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);

                if (!string.IsNullOrEmpty(config.Database.Username) && !string.IsNullOrEmpty(string.Empty))
                {
                    settings.Credential = MongoCredential.CreateCredential(
                        "admin", //TODO: Put into configuration
                        config.Database.Username,
                        config.Database.Password
                    );
                }

                IMongoClient client = new MongoClient(settings);
                IMongoDatabase db = client.GetDatabase(config.Database.DatabaseName);
                return db;
            }).As<IMongoDatabase>().SingleInstance();

            builder.RegisterType<AsymmetricKeyRepository>().As<IAsymmetricKeyRepository>().InstancePerLifetimeScope();
            builder.RegisterType<CertificateAuthorityRepository>().As<ICertificateAuthorityRepository>().InstancePerLifetimeScope();
            builder.RegisterType<CertificateRepository>().As<ICertificateRepository>().InstancePerLifetimeScope();
            builder.RegisterType<RevocatedCertificateRepository>().As<IRevocatedCertificateRepository>().InstancePerLifetimeScope();
        }
    }
}

