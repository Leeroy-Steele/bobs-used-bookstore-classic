using BobsBookstoreClassic.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Login(string redirectUri = null)
        {
            if (string.IsNullOrWhiteSpace(redirectUri)) return RedirectToAction("Index", "Home");
            return Redirect(redirectUri);
        }

        public async Task<IActionResult> LogOut()
        {
            if (BookstoreConfiguration.GetSetting("Services/Authentication") == "aws")
            {
                return await CognitoSignOut();
            }

            return await LocalSignOut();
        }

        private async Task<IActionResult> LocalSignOut()
        {
            Response.Cookies.Delete("LocalAuthentication");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private async Task<IActionResult> CognitoSignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var domain = BookstoreConfiguration.GetSetting("Authentication/Cognito/CognitoDomain");
            var clientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
            var logoutUri = $"{Request.Scheme}://{Request.Host}/";

            return Redirect($"{domain}/logout?client_id={clientId}&logout_uri={logoutUri}");
        }
    }
}
