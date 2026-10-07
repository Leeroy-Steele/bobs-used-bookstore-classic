using Bookstore.Domain.Customers;
using System.Security.Claims;

namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// ASP.NET Core middleware that provides local (development) authentication.
    /// Automatically signs in a hardcoded admin user for local development and testing.
    /// </summary>
    public class LocalAuthenticationMiddleware : IMiddleware
    {
        private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";

        private readonly ICustomerService _customerService;

        public LocalAuthenticationMiddleware(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Path.Value?.StartsWith("/Authentication/Login") == true)
            {
                var principal = CreateClaimsPrincipal();
                context.User = principal;

                await SaveCustomerDetailsAsync(principal);

                var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(1), Path = "/" };
                context.Response.Cookies.Append("LocalAuthentication", "1", cookieOptions);

                context.Response.Redirect("/");
                return;
            }

            if (context.Request.Cookies.ContainsKey("LocalAuthentication"))
            {
                var principal = CreateClaimsPrincipal();
                context.User = principal;
                await SaveCustomerDetailsAsync(principal);
            }

            await next(context);
        }

        private static ClaimsPrincipal CreateClaimsPrincipal()
        {
            var identity = new ClaimsIdentity("Application");
            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));
            return new ClaimsPrincipal(identity);
        }

        private async Task SaveCustomerDetailsAsync(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity;
            if (identity == null) return;

            var dto = new CreateOrUpdateCustomerDto(
                identity.FindFirst("nameidentifier")?.Value ?? string.Empty,
                identity.Name ?? string.Empty,
                identity.FindFirst("given_name")?.Value ?? string.Empty,
                identity.FindFirst("family_name")?.Value ?? string.Empty);

            await _customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}
