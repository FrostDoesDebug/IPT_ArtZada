using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class SellerController : Controller
    {
        private const string UserNameSessionKey = "UserName";
        private const string UserIdSessionKey = "UserId";
        private const string UserRoleSessionKey = "UserRole";
        private const string ClientRole = "Client";
        private const string SellerRole = "Seller";
        private const string AdminRole = "Admin";
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["ArtZadaDb"]?.ConnectionString;

        private int CurrentUserId()
        {
            if (Session[UserIdSessionKey] is int userId && userId > 0)
            {
                return userId;
            }

            var username = Session[UserNameSessionKey] as string;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(ConnectionString))
            {
                return 0;
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 UserId FROM dbo.Users WHERE Username = @Username;";
                cmd.Parameters.AddWithValue("@Username", username.Trim());
                conn.Open();
                var id = cmd.ExecuteScalar();
                if (id is int resolved && resolved > 0)
                {
                    Session[UserIdSessionKey] = resolved;
                    return resolved;
                }
            }

            return 0;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var userName = Session[UserNameSessionKey] as string;
            if (string.IsNullOrWhiteSpace(userName))
            {
                filterContext.Result = RedirectToAction("Login", "Login");
                return;
            }

            var role = (Session[UserRoleSessionKey] as string ?? ClientRole).Trim();

            if (role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = RedirectToAction("UserOverview", "Admin");
                return;
            }

            if (!role.Equals(SellerRole, StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = RedirectToAction("Index", "Client");
                return;
            }

            Session[UserRoleSessionKey] = SellerRole;
            base.OnActionExecuting(filterContext);
        }

        private void SetSellerMode()
        {
            ViewBag.NavMode = "seller";
        }

        public ActionResult Index()
        {
            return RedirectToAction("Store");
        }

        public ActionResult Store()
        {
            SetSellerMode();
            return View();
        }

        public ActionResult AddItem()
        {
            SetSellerMode();
            return View();
        }

        public ActionResult EditItem()
        {
            SetSellerMode();
            return View();
        }

        [HttpGet]
        public JsonResult GetMyItem(int id)
        {
            var sellerId = CurrentUserId();
            if (sellerId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false }, JsonRequestBehavior.AllowGet);
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 1 ProductId, Name, Medium, Price, Stock, Description, ISNULL(ImagePath,'') AS ImagePath
FROM dbo.Products
WHERE ProductId = @ProductId AND SellerId = @SellerId;";
                cmd.Parameters.AddWithValue("@ProductId", id);
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        Response.StatusCode = 404;
                        return Json(new { ok = false }, JsonRequestBehavior.AllowGet);
                    }

                    return Json(new
                    {
                        ok = true,
                        item = new
                        {
                            id = (int)reader["ProductId"],
                            name = (string)reader["Name"],
                            type = (reader["Medium"] as string) ?? string.Empty,
                            price = (decimal)reader["Price"],
                            stocks = (int)reader["Stock"],
                            description = (reader["Description"] as string) ?? string.Empty,
                            photo = string.IsNullOrWhiteSpace(reader["ImagePath"] as string) ? "/Content/images/artzada_logo.png" : (string)reader["ImagePath"]
                        }
                    }, JsonRequestBehavior.AllowGet);
                }
            }
        }

        public ActionResult Message(string withUserId = null)
        {
            SetSellerMode();
            ViewBag.WithUserId = withUserId;
            return View();
        }

        public ActionResult Analytics()
        {
            SetSellerMode();
            ViewBag.Revenue = "100.00";
            return View();
        }

        public ActionResult Account()
        {
            SetSellerMode();
            ViewBag.Username = (Session[UserNameSessionKey] as string) ?? "Seller User";
            ViewBag.IsSeller = true;
            return View();
        }

        [HttpGet]
        public ActionResult SwitchToClient()
        {
            Session[UserRoleSessionKey] = ClientRole;
            return RedirectToAction("Index", "Client");
        }

        public ActionResult Notifications()
        {
            SetSellerMode();
            return View();
        }

        [HttpGet]
        public JsonResult GetMyItems()
        {
            var sellerId = CurrentUserId();
            if (sellerId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, items = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var items = new List<object>();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT ProductId, Name, Medium, Price, Stock, Description, ISNULL(ImagePath,'') AS ImagePath
FROM dbo.Products
WHERE SellerId = @SellerId
ORDER BY ProductId DESC;";
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            id = (int)reader["ProductId"],
                            name = (string)reader["Name"],
                            type = (reader["Medium"] as string) ?? string.Empty,
                            price = (decimal)reader["Price"],
                            stocks = (int)reader["Stock"],
                            description = (reader["Description"] as string) ?? string.Empty,
                            photo = string.IsNullOrWhiteSpace(reader["ImagePath"] as string) ? "/Content/images/artzada_logo.png" : (string)reader["ImagePath"]
                        });
                    }
                }
            }

            return Json(new { ok = true, items }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UploadProductImage(HttpPostedFileBase productImage)
        {
            var sellerId = CurrentUserId();
            if (sellerId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            if (productImage == null || productImage.ContentLength <= 0)
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, message = "No image selected." });
            }

            var ext = Path.GetExtension(productImage.FileName)?.ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif" && ext != ".webp")
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, message = "Unsupported image format." });
            }

            var uploadsDir = Server.MapPath("~/Content/uploads/products");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"product_{sellerId}_{System.Guid.NewGuid():N}{ext}";
            var absPath = Path.Combine(uploadsDir, fileName);
            productImage.SaveAs(absPath);
            var relPath = "/Content/uploads/products/" + fileName;

            return Json(new { ok = true, imageUrl = relPath });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddItemAjax(string name, string type, decimal? price, int? stocks, string description, string photo)
        {
            var sellerId = CurrentUserId();
            if (sellerId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            if (string.IsNullOrWhiteSpace(name) || !price.HasValue || !stocks.HasValue)
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, message = "Name, price, and stocks are required." });
            }

            try
            {
                var safeImage = NormalizeProductImage(photo);
                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
INSERT INTO dbo.Products (SellerId, CategoryId, Name, Description, Price, Stock, ImagePath, Medium, IsAvailable, CreatedAt)
VALUES (@SellerId, 1, @Name, @Description, @Price, @Stock, @ImagePath, @Medium, 1, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                    cmd.Parameters.AddWithValue("@SellerId", sellerId);
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@Description", (object)(description ?? string.Empty));
                    cmd.Parameters.AddWithValue("@Price", price.Value);
                    cmd.Parameters.AddWithValue("@Stock", stocks.Value);
                    cmd.Parameters.AddWithValue("@ImagePath", safeImage);
                    cmd.Parameters.AddWithValue("@Medium", (object)(type ?? string.Empty));
                    conn.Open();
                    var id = (int)cmd.ExecuteScalar();
                    return Json(new { ok = true, id });
                }
            }
            catch
            {
                Response.StatusCode = 500;
                return Json(new { ok = false, message = "Failed to save item. Try a smaller photo or use a file path." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateItemAjax(int id, string name, string type, decimal? price, int? stocks, string description, string photo)
        {
            var sellerId = CurrentUserId();
            if (sellerId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            try
            {
                var safeImage = NormalizeProductImage(photo);
                using (var conn = new SqlConnection(ConnectionString))
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE dbo.Products
SET Name = @Name,
    Medium = @Medium,
    Price = @Price,
    Stock = @Stock,
    Description = @Description,
    ImagePath = @ImagePath,
    UpdatedAt = GETDATE()
WHERE ProductId = @ProductId AND SellerId = @SellerId;";
                    cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Medium", type ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Price", price ?? 0m);
                    cmd.Parameters.AddWithValue("@Stock", stocks ?? 0);
                    cmd.Parameters.AddWithValue("@Description", description ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ImagePath", safeImage);
                    cmd.Parameters.AddWithValue("@ProductId", id);
                    cmd.Parameters.AddWithValue("@SellerId", sellerId);
                    conn.Open();
                    var affected = cmd.ExecuteNonQuery();
                    if (affected <= 0)
                    {
                        Response.StatusCode = 404;
                        return Json(new { ok = false, message = "Item not found." });
                    }
                }
            }
            catch
            {
                Response.StatusCode = 500;
                return Json(new { ok = false, message = "Failed to update item photo/details." });
            }

            return Json(new { ok = true });
        }

        private string NormalizeProductImage(string photo)
        {
            const string fallback = "/Content/images/artzada_logo.png";
            if (string.IsNullOrWhiteSpace(photo))
            {
                return fallback;
            }

            var value = photo.Trim();
            if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            if (value.Length > 280)
            {
                return fallback;
            }

            return value;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteItemsAjax(int[] ids)
        {
            var sellerId = CurrentUserId();
            if (sellerId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            if (ids == null || ids.Length == 0)
            {
                return Json(new { ok = true, deleted = 0 });
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                var placeholders = ids.Select((_, i) => "@Id" + i).ToArray();
                cmd.CommandText = $"DELETE FROM dbo.Products WHERE SellerId = @SellerId AND ProductId IN ({string.Join(",", placeholders)});";
                cmd.Parameters.AddWithValue("@SellerId", sellerId);
                for (var i = 0; i < ids.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@Id" + i, ids[i]);
                }
                conn.Open();
                var affected = cmd.ExecuteNonQuery();
                return Json(new { ok = true, deleted = affected });
            }
        }
    }
}
