using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class AdminController : Controller
    {
        private const string AdminModeSessionKey = "AdminMode";
        private const string IsAdminSessionKey = "IsAdmin";
        private const string AdminUserNameSessionKey = "AdminUserName";

        // Demo credentials
        private const string DemoAdminUsername = "admin";
        private const string DemoAdminPassword = "admin123";

        private bool IsAdminModeEnabled()
        {
            return Session[AdminModeSessionKey] is bool enabled && enabled;
        }

        private bool IsAdminLoggedIn()
        {
            return Session[IsAdminSessionKey] is bool isAdmin && isAdmin;
        }

        private ActionResult RedirectToPublicLanding()
        {
            return RedirectToAction("Landing", "Landing");
        }

        public ActionResult Index()
        {
            return RedirectToAction("Landing");
        }

        [HttpGet]
        public ActionResult Enable()
        {
            Session[AdminModeSessionKey] = true;
            return RedirectToAction("Landing");
        }

        [HttpGet]
        public ActionResult Landing()
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!string.Equals(model.Username, DemoAdminUsername) || !string.Equals(model.Password, DemoAdminPassword))
            {
                ModelState.AddModelError("", "Invalid admin username or password.");
                return View(model);
            }

            Session[IsAdminSessionKey] = true;
            Session[AdminUserNameSessionKey] = DemoAdminUsername;

            return RedirectToAction("UserOverview");
        }

        public ActionResult UserOverview()
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult Pending()
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult BannedListed()
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult Account()
        {
            if (!IsAdminModeEnabled())
            {
                return RedirectToPublicLanding();
            }

            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult Logout()
        {
            Session.Remove(IsAdminSessionKey);
            Session.Remove(AdminUserNameSessionKey);
            return RedirectToPublicLanding();
        }
    }
}