using System.IO;
using System.Security.Claims;
using Amazon.Rekognition;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using BobsBookstoreClassic.Data;
using Bookstore.Common;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Web;
using NLogTarget = NLog.Targets.Target;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────────────────
// Initialize BookstoreConfiguration from IConfiguration so legacy static
// accessor calls (BookstoreConfiguration.GetSetting / GetConnectionString)
// continue to work throughout the codebase.
BookstoreConfiguration.Initialize(builder.Configuration);

// Load additional settings from AWS SSM when running in AWS mode
LoadAwsSsmParameters(builder.Configuration);

// ── Logging (NLog) ─────────────────────────────────────────────────────────
ConfigureNLog(builder.Configuration);
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// ── Autofac DI ─────────────────────────────────────────────────────────────
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureAutofac);

// ── MVC + HttpContextAccessor ──────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// ── Authentication ─────────────────────────────────────────────────────────
var authMode = builder.Configuration["Services:Authentication"];

if (authMode == "aws")
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Cognito:LocalClientId"];
        options.MetadataAddress = builder.Configuration["Authentication:Cognito:MetadataAddress"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;
        options.UseTokenLifetime = false;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "cognito:username",
            RoleClaimType = "cognito:groups"
        };

        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                var request = context.HttpContext.Request;
                var returnUrl = $"{request.Scheme}://{request.Host}/signin-oidc";
                context.ProtocolMessage.RedirectUri = returnUrl;
                return Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                var request = context.HttpContext.Request;
                var returnUrl = $"{request.Scheme}://{request.Host}/signin-oidc";
                context.TokenEndpointRequest!.RedirectUri = returnUrl;
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var customerService = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();

                var principal = new ClaimsPrincipal(context.Principal!.Identity!);
                var identity = (ClaimsIdentity)principal.Identity!;

                var dto = new CreateOrUpdateCustomerDto(
                    identity.GetSub(),
                    identity.Name!,
                    identity.FindFirst(y => y.Type.Contains("givenname"))!.Value,
                    identity.FindFirst(y => y.Type.Contains("surname"))!.Value);

                await customerService.CreateOrUpdateCustomerAsync(dto);
            }
        };
    });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie();
}

// ── Authorization (replaces global AuthorizeAttribute filter) ──────────────
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ── Build ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────
app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();

// Conditionally add LocalAuthenticationMiddleware for non-AWS auth
if (authMode != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.UseAuthorization();

// ── Endpoint Routing ───────────────────────────────────────────────────────
app.MapAreaControllerRoute(
    name: "Admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ═══════════════════════════════════════════════════════════════════════════
// Static helper methods
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Configures all Autofac service registrations (migrated from DependencyInjectionSetup.cs).
/// </summary>
static void ConfigureAutofac(HostBuilderContext context, ContainerBuilder builder)
{
    var configuration = context.Configuration;

    // Services
    builder.RegisterType<BookService>().As<IBookService>();
    builder.RegisterType<OrderService>().As<IOrderService>();
    builder.RegisterType<ReferenceDataService>().As<IReferenceDataService>();
    builder.RegisterType<OfferService>().As<IOfferService>();
    builder.RegisterType<CustomerService>().As<ICustomerService>();
    builder.RegisterType<AddressService>().As<IAddressService>();
    builder.RegisterType<ShoppingCartService>().As<IShoppingCartService>();
    builder.RegisterType<ImageResizeService>().As<IImageResizeService>();

    // DbContext — register with options using the connection string
    var connectionString = configuration.GetConnectionString("BookstoreDatabaseConnection");
    builder.Register(c =>
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new ApplicationDbContext(optionsBuilder.Options);
    }).AsSelf().InstancePerLifetimeScope();

    // Repositories
    builder.RegisterType<CustomerRepository>().As<ICustomerRepository>();
    builder.RegisterType<AddressRepository>().As<IAddressRepository>();
    builder.RegisterType<BookRepository>().As<IBookRepository>();
    builder.RegisterType<OfferRepository>().As<IOfferRepository>();
    builder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>();
    builder.RegisterType<OrderRepository>().As<IOrderRepository>();
    builder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>();

    // Open generic
    builder.RegisterGeneric(typeof(PaginatedList<>)).As(typeof(IPaginatedList<>)).InstancePerLifetimeScope();

    // Conditional: File service
    if (configuration["Services:FileService"] == "aws")
    {
        builder.RegisterType<AmazonS3Client>().As<IAmazonS3>().SingleInstance();
        builder.RegisterType<S3FileService>().As<IFileService>();
    }
    else
    {
        var webRootPath = context.HostingEnvironment.ContentRootPath;
        var contentPath = Path.Combine(webRootPath, "wwwroot");
        builder.RegisterInstance(new LocalFileService(contentPath)).As<IFileService>();
    }

    // Conditional: Image validation service
    if (configuration["Services:ImageValidationService"] == "aws")
    {
        builder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>().SingleInstance();
        builder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>();
    }
    else
    {
        builder.RegisterType<LocalImageValidationService>().As<IImageValidationService>();
    }
}

/// <summary>
/// Loads additional configuration parameters from AWS SSM Parameter Store
/// (migrated from ConfigurationSetup.cs).
/// </summary>
static void LoadAwsSsmParameters(IConfiguration configuration)
{
    var rootPath = "/" + Constants.AppName;

    const string databasePath = "/Database";
    const string authenticationPath = "/Authentication";
    const string fileServicePath = "/Files";

    if (configuration["Services:Database"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParameterRequest
        {
            Name = $"{rootPath}{databasePath}/ConnectionStrings/BookstoreDatabaseConnection"
        };
        var response = client.GetParameterAsync(request).GetAwaiter().GetResult();
        BookstoreConfiguration.AddSetting(
            response.Parameter.Name.Replace($"{rootPath}{databasePath}/", string.Empty),
            response.Parameter.Value);
    }

    if (configuration["Services:Authentication"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParametersByPathRequest
        {
            Path = $"{rootPath}{authenticationPath}/",
            Recursive = true
        };
        var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
        foreach (var parameter in response.Parameters)
        {
            BookstoreConfiguration.AddSetting(
                parameter.Name.Replace($"{rootPath}/", string.Empty),
                parameter.Value);
        }
    }

    if (configuration["Services:FileService"] == "aws")
    {
        using var client = new AmazonSimpleSystemsManagementClient();
        var request = new GetParametersByPathRequest
        {
            Path = $"{rootPath}{fileServicePath}/",
            Recursive = true
        };
        var response = client.GetParametersByPathAsync(request).GetAwaiter().GetResult();
        foreach (var parameter in response.Parameters)
        {
            BookstoreConfiguration.AddSetting(
                parameter.Name.Replace($"{rootPath}/", string.Empty),
                parameter.Value);
        }
    }
}

/// <summary>
/// Configures NLog programmatically (migrated from LoggingSetup.cs).
/// </summary>
static void ConfigureNLog(IConfiguration configuration)
{
    var config = new LoggingConfiguration();

    NLogTarget loggingTarget;

    if (configuration["Services:LoggingService"] == "aws")
    {
        loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
    }
    else
    {
        loggingTarget = new NLog.Targets.DebuggerTarget();
    }

    config.AddTarget("aws", loggingTarget);
    config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));

    LogManager.Configuration = config;
}
