using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Helpers;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class AdminController : Controller
    {
        private const string IsAdminSessionKey = "IsAdmin";
        private const string AdminUserNameSessionKey = "AdminUserName";
        private const string UserNameSessionKey = "UserName";
        private const string UserIdSessionKey = "UserId";
        private const string UserRoleSessionKey = "UserRole";
        private const string AdminRole = "Admin";

        private const string DemoAdminUsername = "admin";
        private const string DemoAdminPassword = "admin123";

        private static string ConnectionString => ConfigurationManager.ConnectionStrings["ArtZadaDb"]?.ConnectionString;

        private bool IsAdminLoggedIn()
        {
            return string.Equals(Session[UserRoleSessionKey] as string, AdminRole, StringComparison.OrdinalIgnoreCase);
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

            var isDemoAdmin = string.Equals(model.Username, DemoAdminUsername) && string.Equals(model.Password, DemoAdminPassword);
            var isDbAdmin = TryValidateDbAdmin(model.Username, model.Password, out var dbUserId, out var dbUsername);
            if (!isDemoAdmin && !isDbAdmin)
            {
                ModelState.AddModelError("", "Invalid admin username or password.");
                return View(model);
            }

            Session[IsAdminSessionKey] = true;
            Session[AdminUserNameSessionKey] = isDbAdmin ? dbUsername : DemoAdminUsername;
            Session[UserNameSessionKey] = isDbAdmin ? dbUsername : DemoAdminUsername;
            if (isDbAdmin) Session[UserIdSessionKey] = dbUserId;
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
            Session.Remove(UserIdSessionKey);
            Session.Remove(UserNameSessionKey);
            Session.Remove(UserRoleSessionKey);
            return RedirectToPublicLanding();
        }

        [HttpGet]
        public JsonResult GetPendingUsers()
        {
            return LoadUsersByStatus("Pending");
        }

        [HttpGet]
        public JsonResult GetActiveUsers()
        {
            return LoadUsersByStatus("Active");
        }

        [HttpGet]
        public JsonResult GetBannedUsers()
        {
            return LoadUsersByStatus("Banned");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ApproveUser(int userId)
        {
            return SetUserStatus(userId, "Active", null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeclineUser(int userId)
        {
            return SetUserStatus(userId, "Banned", "Declined by admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult BanUser(int userId, string reason)
        {
            return SetUserStatus(userId, "Banned", string.IsNullOrWhiteSpace(reason) ? "Banned by admin" : reason.Trim());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UnbanUser(int userId)
        {
            return SetUserStatus(userId, "Active", null);
        }

        private JsonResult LoadUsersByStatus(string status)
        {
            if (!IsAdminLoggedIn())
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, users = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var users = new List<object>();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT UserId, Username, Role, ISNULL(ProfileImage,'') AS ProfileImage, ISNULL(BanReason,'') AS BanReason
FROM dbo.Users
WHERE AccountStatus = @Status
ORDER BY UserId DESC;";
                cmd.Parameters.AddWithValue("@Status", status);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new
                        {
                            userId = (int)reader["UserId"],
                            username = (string)reader["Username"],
                            role = (reader["Role"] as string) ?? "User",
                            avatar = string.IsNullOrWhiteSpace(reader["ProfileImage"] as string) ? "/Content/images/artzada_logo.png" : (string)reader["ProfileImage"],
                            reason = (reader["BanReason"] as string) ?? string.Empty
                        });
                    }
                }
            }

            return Json(new { ok = true, users }, JsonRequestBehavior.AllowGet);
        }

        private JsonResult SetUserStatus(int userId, string status, string reason)
        {
            if (!IsAdminLoggedIn())
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.Users
SET AccountStatus = @Status,
    BanReason = @Reason,
    UpdatedAt = GETDATE()
WHERE UserId = @UserId;";
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@Reason", (object)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                var affected = cmd.ExecuteNonQuery();
                if (affected <= 0)
                {
                    Response.StatusCode = 404;
                    return Json(new { ok = false, message = "User not found." });
                }
            }

            return Json(new { ok = true });
        }

        private bool TryValidateDbAdmin(string username, string password, out int userId, out string resolvedUsername)
        {
            userId = 0;
            resolvedUsername = null;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(ConnectionString))
            {
                return false;
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 UserId, Username, PasswordHash
FROM dbo.Users
WHERE Username = @Username
  AND Role = 'Admin'
  AND IsActive = 1
  AND AccountStatus <> 'Banned';";
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return false;
                    var stored = reader["PasswordHash"] as string;
                    if (string.IsNullOrWhiteSpace(stored)) return false;
                    var valid = Crypto.VerifyHashedPassword(stored, password) || string.Equals(stored, password, StringComparison.Ordinal);
                    if (!valid) return false;
                    userId = (int)reader["UserId"];
                    resolvedUsername = (string)reader["Username"];
                    return true;
                }
            }
        }
    }
}
