using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class SignupViewModel
    {
        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    public static class InMemoryUserStore
    {
        private sealed class UserAccount
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private static readonly object _lock = new object();
        private static readonly Dictionary<string, UserAccount> _users =
            new Dictionary<string, UserAccount>(StringComparer.OrdinalIgnoreCase);

        static InMemoryUserStore()
        {
            _users["demo"] = new UserAccount
            {
                Username = "demo",
                Password = "demo",
                FirstName = "Demo",
                LastName = "User"
            };
        }

        public static bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            lock (_lock)
            {
                UserAccount user;
                return _users.TryGetValue(username.Trim(), out user) && string.Equals(user.Password, password);
            }
        }

        public static bool TryCreateUser(SignupViewModel model, out string error)
        {
            error = null;

            if (model == null)
            {
                error = "Invalid signup details.";
                return false;
            }

            var username = (model.Username ?? string.Empty).Trim();
            if (username.Length == 0)
            {
                error = "Username is required.";
                return false;
            }

            lock (_lock)
            {
                if (_users.ContainsKey(username))
                {
                    error = "Username is already taken.";
                    return false;
                }

                _users[username] = new UserAccount
                {
                    Username = username,
                    Password = model.Password,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                return true;
            }
        }
    }

    public class LoginController : Controller
    {
        // Temporary bypass switch for development/demo.
        // Set to false once you implement real authentication (DB + hashed passwords, etc.).
        private const bool BYPASS_AUTH = false;

        private const string UserNameSessionKey = "UserName";
        private const string UserRoleSessionKey = "UserRole";
        private const string ClientRole = "Client";
        private const string SellerRole = "Seller";
        private const string AdminRole = "Admin";

        private bool IsUserLoggedIn()
        {
            return !string.IsNullOrWhiteSpace(Session[UserNameSessionKey] as string)
                || (Session["IsAdmin"] is bool isAdmin && isAdmin);
        }

        private ActionResult RedirectToRoleHome()
        {
            var role = (Session[UserRoleSessionKey] as string ?? ClientRole).Trim();

            if (role.Equals(SellerRole, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Store", "Seller");
            }

            if (role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("UserOverview", "Admin");
            }

            return RedirectToAction("Index", "Client");
        }

        [HttpGet]
        public ActionResult Login()
        {
            if (IsUserLoggedIn())
            {
                return RedirectToRoleHome();
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

            // BYPASS (TEMP): allow any credentials to log in while backend auth is not yet implemented.
            // Remove/disable this block later.
            if (BYPASS_AUTH)
            {
                Session[UserNameSessionKey] = string.IsNullOrWhiteSpace(model.Username) ? "demo" : model.Username;
                Session[UserRoleSessionKey] = ClientRole;
                return RedirectToRoleHome();
            }

            if (!InMemoryUserStore.ValidateCredentials(model.Username, model.Password))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            Session[UserNameSessionKey] = model.Username;
            Session[UserRoleSessionKey] = ClientRole;
            return RedirectToRoleHome();
        }

        [HttpGet]
        public ActionResult Signup()
        {
            if (IsUserLoggedIn())
            {
                return RedirectToRoleHome();
            }

            return View(new SignupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Signup(SignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string error;
            if (!InMemoryUserStore.TryCreateUser(model, out error))
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            Session[UserNameSessionKey] = model.Username;
            Session[UserRoleSessionKey] = ClientRole;
            return RedirectToRoleHome();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Landing", "Landing");
        }
    }
}