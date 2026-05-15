using System;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Helpers;
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
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginController : Controller
    {
        private const string UserNameSessionKey = "UserName";
        private const string UserIdSessionKey = "UserId";
        private const string UserRoleSessionKey = "UserRole";
        private const string ClientRole = "Client";
        private const string SellerRole = "Seller";
        private const string AdminRole = "Admin";

        private static string ConnectionString =>
            ConfigurationManager.ConnectionStrings["ArtZadaDb"]?.ConnectionString;

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

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                ModelState.AddModelError("", "Database connection is not configured.");
                return View(model);
            }

            var usernameInput = (model.Username ?? string.Empty).Trim();
            var passwordInput = model.Password ?? string.Empty;

            string resolvedUsername;
            int resolvedUserId;
            string resolvedRole;
            if (!TryAuthenticateUser(usernameInput, passwordInput, out resolvedUserId, out resolvedUsername, out resolvedRole))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            Session[UserIdSessionKey] = resolvedUserId;
            Session[UserNameSessionKey] = resolvedUsername;
            Session[UserRoleSessionKey] = resolvedRole;
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

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                ModelState.AddModelError("", "Database connection is not configured.");
                return View(model);
            }

            string error;
            int createdUserId;
            if (!TryCreateUser(model, out createdUserId, out error))
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            Session[UserIdSessionKey] = createdUserId;
            Session[UserNameSessionKey] = model.Username.Trim();
            Session[UserRoleSessionKey] = ClientRole;
            return RedirectToRoleHome();
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Landing", "Landing");
        }

        private static bool TryAuthenticateUser(string username, string password, out int resolvedUserId, out string resolvedUsername, out string resolvedRole)
        {
            resolvedUserId = 0;
            resolvedUsername = null;
            resolvedRole = ClientRole;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 UserId, Username, PasswordHash, Role
FROM dbo.Users
WHERE Username = @username
  AND IsActive = 1
  AND AccountStatus <> 'Banned';";
                cmd.Parameters.AddWithValue("@username", username);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return false;
                    }

                    var storedPassword = reader["PasswordHash"] as string;
                    if (string.IsNullOrWhiteSpace(storedPassword))
                    {
                        return false;
                    }

                    var passwordValid =
                        Crypto.VerifyHashedPassword(storedPassword, password)
                        || string.Equals(storedPassword, password, StringComparison.Ordinal);

                    if (!passwordValid)
                    {
                        return false;
                    }

                    resolvedUserId = reader["UserId"] is int id ? id : 0;
                    resolvedUsername = (reader["Username"] as string) ?? username;
                    resolvedRole = MapDbRoleToSessionRole((reader["Role"] as string) ?? "Buyer");
                    if (resolvedUserId <= 0)
                    {
                        return false;
                    }
                    return true;
                }
            }
        }

        private static bool TryCreateUser(SignupViewModel model, out int createdUserId, out string error)
        {
            createdUserId = 0;
            error = null;

            var username = (model.Username ?? string.Empty).Trim();
            var email = (model.Email ?? string.Empty).Trim();
            var fullName = string.Format("{0} {1}", model.FirstName?.Trim(), model.LastName?.Trim()).Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                error = "Username is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                error = "Email is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                error = "Full name is required.";
                return false;
            }

            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.Users
    (Username, Email, PasswordHash, FullName, Role, AccountStatus, IsSeller, IsActive, CreatedAt)
VALUES
    (@Username, @Email, @PasswordHash, @FullName, 'Buyer', 'Active', 0, 1, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@Username", username);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@PasswordHash", Crypto.HashPassword(model.Password ?? string.Empty));
                    cmd.Parameters.AddWithValue("@FullName", fullName);

                    conn.Open();
                    var createdId = cmd.ExecuteScalar();
                    if (createdId is int)
                    {
                        createdUserId = (int)createdId;
                        return createdUserId > 0;
                    }

                    error = "Failed to create account.";
                    return false;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                error = "Username or email is already taken.";
                return false;
            }
            catch
            {
                error = "Failed to create account.";
                return false;
            }
        }

        private static string MapDbRoleToSessionRole(string dbRole)
        {
            if (string.Equals(dbRole, "Seller", StringComparison.OrdinalIgnoreCase))
            {
                return SellerRole;
            }

            if (string.Equals(dbRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return AdminRole;
            }

            return ClientRole;
        }
    }
}
