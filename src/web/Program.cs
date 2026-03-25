using Autofac;
using Autofac.Extensions.DependencyInjection;
using log4net;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using Notary;
using Notary.Configuration;
using Notary.Service;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

var config = new NotaryConfiguration();
SetEnvironmentVariables(config);

builder.Host.ConfigureContainer<ContainerBuilder>(c =>
{
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

if (config.Authentication is AuthenticationProvider.OpenId)
{
    if (config.OpenId == null)
        throw new NotSupportedException("OpenId configuration is not provided.");

    builder.Services.AddAuthentication().AddOpenIdConnect(o =>
    {
        o.ClientId = config.OpenId.ClientId;
        o.MetadataAddress = config.OpenId.MetadataAddress;

        if (!string.IsNullOrEmpty(config.OpenId.ClientSecret))
            o.ClientSecret = config.OpenId.ClientSecret;
    });
}
else if (config.Authentication is AuthenticationProvider.ActiveDirectory or AuthenticationProvider.System)
{
    if (config.TokenSettings == null)
        throw new NotaryException("Please provide token settings for system or AD authentication");

    builder.Services.AddAuthentication().AddJwtBearer(a =>
    {
        a.RequireHttpsMetadata = false;
        a.Audience = config.TokenSettings.Audience;
        a.Authority = config.TokenSettings.Authority;
        a.ClaimsIssuer = config.TokenSettings.Issuer;
    });
}
else
{
    throw new NotaryException("Something went wrong");
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
    NotaryConfiguration.SetPropertiesEnvVariable(config);

    // Set database environment variables
    NotaryConfiguration.SetPropertiesEnvVariable(config.Database);

    // Set OpenID environment variables
    NotaryConfiguration.SetPropertiesEnvVariable(config.OpenId);
}