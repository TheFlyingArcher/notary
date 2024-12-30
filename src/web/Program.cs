using Auth0.AspNetCore.Authentication;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using log4net;

using Microsoft.AspNetCore.HttpOverrides;

using MudBlazor.Services;

using Notary.Configuration;
using Notary.Service;

using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
var config = builder.Configuration
    .GetSection("Notary")
    .Get<NotaryConfiguration>();

if (config == null)
    throw new InvalidOperationException("Configuration not found. Please ensure a configuration file is present.");

builder.Host.ConfigureContainer<ContainerBuilder>(c =>
{
    SetEnvironmentVariables(config);
    c.RegisterInstance(config).SingleInstance();

    c.Register(r => LogManager.GetLogger(typeof(Program))).As<ILog>().SingleInstance();
    RegisterModules.Register(c);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();

if (config.OpenId != null)
{
    builder.Services.AddAuth0WebAppAuthentication(o =>
    {
        o.Domain = config.OpenId.Domain;
        o.ClientId = config.OpenId.ClientId;
    });
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseForwardedHeaders();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

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
    var typeProperties = type.GetProperties(BindingFlags.Public);

    foreach (var p in typeProperties)
    {
        var attribute = p.GetCustomAttribute<NotaryEnvironmentVariableAttribute>();
        if (attribute == null)
            continue;

        var evValue = Environment.GetEnvironmentVariable(attribute.EnvironmentVariable);
        if (!string.IsNullOrEmpty(evValue))
        {
            p.SetValue(type, evValue);
        }
    }
}