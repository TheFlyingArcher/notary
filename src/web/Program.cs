using Auth0.AspNetCore.Authentication;

using Autofac;
using Autofac.Extensions.DependencyInjection;

using log4net;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

using MudBlazor.Services;

using Notary.Configuration;
using Notary.Service;

using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
var config = builder.Configuration
    .GetSection("Notary")
    .Get<NotaryConfiguration>();

if (config == null)
    throw new InvalidOperationException("Configuration not found. Please ensure a configuration file is present.");
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

builder.Services.AddAuth0WebAppAuthentication(o =>
{
    o.Domain = builder.Configuration["Auth0:Domain"];
    o.ClientId = builder.Configuration["Auth0:ClientId"];
});

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
