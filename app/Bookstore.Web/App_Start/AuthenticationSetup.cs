using System.Security.Claims;
using System.Threading.Tasks;
using BobsBookstoreClassic.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Web
{
    public static class AuthenticationConfig
    {
        public static void ConfigureAuthentication(IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            if (BookstoreConfiguration.TryGetSetting("Services/Authentication") == "aws")
            {
                ConfigureCognitoAuthentication(services);
            }
            else
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie();
            }
        }

        private static void ConfigureCognitoAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = System.TimeSpan.FromDays(30);
            })
            .AddOpenIdConnect(options =>
            {
                options.ClientId = BookstoreConfiguration.TryGetSetting("Authentication/Cognito/LocalClientId");
                options.MetadataAddress = BookstoreConfiguration.TryGetSetting("Authentication/Cognito/MetadataAddress");
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.SaveTokens = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "cognito:username",
                    RoleClaimType = "cognito:groups"
                };
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}/signin-oidc";
                        context.ProtocolMessage.RedirectUri = returnUrl;
                        return Task.CompletedTask;
                    },
                    OnAuthorizationCodeReceived = context =>
                    {
                        var returnUrl = $"{context.Request.Scheme}://{context.Request.Host}/signin-oidc";
                        context.TokenEndpointRequest.RedirectUri = returnUrl;
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var service = context.HttpContext.RequestServices
                            .GetRequiredService<Bookstore.Domain.Customers.ICustomerService>();
                        var identity = (ClaimsIdentity)context.Principal.Identity;
                        var dto = new Bookstore.Domain.Customers.CreateOrUpdateCustomerDto(
                            identity.FindFirst(x => x.Type.Contains("nameidentifier"))?.Value,
                            identity.Name,
                            identity.FindFirst(x => x.Type.Contains("givenname"))?.Value,
                            identity.FindFirst(x => x.Type.Contains("surname"))?.Value);
                        await service.CreateOrUpdateCustomerAsync(dto);
                    }
                };
            });
        }
    }
}
