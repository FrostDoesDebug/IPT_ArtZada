using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class AdminController : Controller
    {
        private const string IsAdminSessionKey = "IsAdmin";
        private const string AdminUserNameSessionKey = "AdminUserName";
        private const string UserNameSessionKey = "UserName";
        private const string UserRoleSessionKey = "UserRole";
        private const string AdminRole = "Admin";

        // Demo credentials
        private const string DemoAdminUsername = "admin";
        private const string DemoAdminPassword = "admin123";

        private bool IsAdminLoggedIn()
        {
            return Session[IsAdminSessionKey] is bool isAdmin
                && isAdmin
                && string.Equals(Session[UserRoleSessionKey] as string, AdminRole, StringComparison.OrdinalIgnoreCase);
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
            if (IsAdminLoggedIn())
            {
                return RedirectToAction("UserOverview");
            }

            return RedirectToAction("Landing");
        }

        [HttpGet]
        public ActionResult Landing()
        {
            if (IsAdminLoggedIn())
            {
                return RedirectToAction("UserOverview");
            }

            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            if (IsAdminLoggedIn())
            {
                return RedirectToAction("UserOverview");
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
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
            Session[UserNameSessionKey] = DemoAdminUsername;
            Session[UserRoleSessionKey] = AdminRole;

            return RedirectToAction("UserOverview");
        }

        public ActionResult UserOverview()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult Pending()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult BannedListed()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult BanList()
        {
            return RedirectToAction("BannedListed");
        }

        public ActionResult Account()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        public ActionResult Notifications()
        {
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
            Session.Remove(UserNameSessionKey);
            Session.Remove(UserRoleSessionKey);
            return RedirectToPublicLanding();
        }
    }
}