using System.Reflection;
using Autofac;
using log4net;
using Notary.Configuration;
using Notary.Interface.Service;
using Notary.Service;

WriteLine("Welcome!", ConsoleColor.Green);

try
{
    var config = new NotaryConfiguration()
    {
        ActiveDirectory = new NotaryActiveDirectoryConfiguration(),
        Authentication = Notary.AuthenticationProvider.OpenId,
        Database = new NotaryDatabaseConfiguration(),
        OpenId = new NotaryOpenIdConfiguration(),
        TokenSettings = new NotaryTokenSettingsConfiguration()
    };

    SetEnvironmentVariables(config);

    var builder = new ContainerBuilder();
    _ = builder.RegisterInstance(config).SingleInstance();
    builder.Register(r => LogManager.GetLogger(typeof(Program))).As<ILog>().SingleInstance();

    RegisterModules.Register(builder);

    WriteLine("Building container...");
    var container = builder.Build();

    WriteLine("Initializing AsymmetricKeys...");
    var asymmetricKeyService = container.Resolve<IAsymmetricKeyService>();
    await asymmetricKeyService.InitializeAsync();

    WriteLine("Initializing Certificates...");
    var certificateService = container.Resolve<ICertificateService>();
    await certificateService.InitializeAsync();

    WriteLine("Initializing Certificate Authorities...");
    var caService = container.Resolve<ICertificateAuthorityService>();
    await caService.InitializeAsync();

    WriteLine("Success!", ConsoleColor.Green);
}
catch (Exception ex)
{
    Write("ERROR! ", ConsoleColor.Red);
    Write("Something went wrong ");
    WriteLine(ex.Message);
    Console.Error.WriteLine(ex.StackTrace);
}

static void Write(string text, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    Console.Write(text);
    Console.ForegroundColor = ConsoleColor.Gray;
}

static void WriteLine(string line, ConsoleColor color = ConsoleColor.Gray)
{
    Console.ForegroundColor = color;
    Console.WriteLine(line);
    Console.ForegroundColor = ConsoleColor.Gray;
}

static void SetEnvironmentVariables(NotaryConfiguration config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    // Set root environment variables
    SetPropertiesEnvVariable(config);

    // Set database environment variables
    SetPropertiesEnvVariable(config.Database);

    // Set OpenID environment variables
    SetPropertiesEnvVariable(config.OpenId);
}

static void SetPropertiesEnvVariable<T>(T configType) where T : class
{
    if (configType == null)
        return;

    var type = configType.GetType();
    var typeProperties = type.GetProperties();

    foreach (var p in typeProperties)
    {
        var attribute = p.GetCustomAttribute<NotaryEnvironmentVariableAttribute>();
        if (attribute == null)
            continue;

        var evValue = Environment.GetEnvironmentVariable(attribute.EnvironmentVariable);
        if (!string.IsNullOrEmpty(evValue))
        {
            p.SetValue(configType, evValue);
        }
    }
}