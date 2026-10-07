using System;
using BobsBookstoreClassic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Controllers
{
    [AllowAnonymous]
    public class AuthenticationController : Controller
    {
        public ActionResult Login(string redirectUri = null)
        {
            if (string.IsNullOrWhiteSpace(redirectUri)) return RedirectToAction("Index", "Home");
            return Redirect(redirectUri);
        }

        public ActionResult LogOut()
        {
            return BookstoreConfiguration.TryGetSetting("Services/Authentication") == "aws"
                ? CognitoSignOut()
                : LocalSignOut();
        }

        private ActionResult LocalSignOut()
        {
            Response.Cookies.Delete("LocalAuthentication");
            return RedirectToAction("Index", "Home");
        }

        private ActionResult CognitoSignOut()
        {
            Response.Cookies.Delete(".AspNet.Cookies");
            var domain = BookstoreConfiguration.TryGetSetting("Authentication/Cognito/CognitoDomain");
            var clientId = BookstoreConfiguration.TryGetSetting("Authentication/Cognito/LocalClientId");
            var logoutUri = $"{Request.Scheme}://{Request.Host}/";
            return Redirect($"{domain}/logout?client_id={clientId}&logout_uri={logoutUri}");
        }
    }
}
