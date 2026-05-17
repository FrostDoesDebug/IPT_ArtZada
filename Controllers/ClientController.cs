using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class ClientController : Controller
    {
        private const string UserNameSessionKey = "UserName";
        private const string UserIdSessionKey = "UserId";
        private const string UserRoleSessionKey = "UserRole";
        private const string ClientRole = "Client";
        private const string SellerRole = "Seller";
        private const string AdminRole = "Admin";
        private static string ConnectionString => ConfigurationManager.ConnectionStrings["ArtZadaDb"]?.ConnectionString;

        private sealed class CartRow
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
            public string ArtistName { get; set; }
            public string ImagePath { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }
        private sealed class ProductCard
        {
            public int ProductId { get; set; }
            public string Name { get; set; }
            public string Medium { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public bool IsFlashSale { get; set; }
            public bool IsAvailable { get; set; }
            public string ImagePath { get; set; }
        }

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

        private int EnsureCart(int userId, SqlConnection conn)
        {
            using (var find = conn.CreateCommand())
            {
                find.CommandText = "SELECT TOP 1 CartId FROM dbo.Carts WHERE UserId = @UserId;";
                find.Parameters.AddWithValue("@UserId", userId);
                var existing = find.ExecuteScalar();
                if (existing is int cartId && cartId > 0)
                {
                    return cartId;
                }
            }

            using (var create = conn.CreateCommand())
            {
                create.CommandText = @"
INSERT INTO dbo.Carts (UserId, CreatedAt)
VALUES (@UserId, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                create.Parameters.AddWithValue("@UserId", userId);
                return (int)create.ExecuteScalar();
            }
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

            if (role.Equals(SellerRole, StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Result = RedirectToAction("Store", "Seller");
                return;
            }

            Session[UserRoleSessionKey] = ClientRole;
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Message(string withUserId = null)
        {
            ViewBag.WithUserId = withUserId;
            return View();
        }

        public ActionResult Analytics()
        {
            ViewBag.TotalSpent = "2450.00";
            ViewBag.OrdersThisMonth = 8;
            return View();
        }

        public ActionResult Account()
        {
            ViewBag.Username = (Session[UserNameSessionKey] as string) ?? "Client User";
            ViewBag.IsSeller = false;
            return View();
        }

        [HttpGet]
        public ActionResult SwitchToSeller()
        {
            Session[UserRoleSessionKey] = SellerRole;
            return RedirectToAction("Store", "Seller");
        }

        public ActionResult Notifications()
        {
            return View();
        }

        public ActionResult Product(int? id)
        {
            ViewBag.ProductId = id ?? 0;
            return View();
        }

        public ActionResult Cart()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddToCart(int productId, int quantity = 1)
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "You must be logged in." });
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            try
            {
                using (var conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    using (var existsCmd = conn.CreateCommand())
                    {
                        existsCmd.CommandText = @"
SELECT TOP 1 ProductId, Stock
FROM dbo.Products
WHERE ProductId = @ProductId AND IsAvailable = 1;";
                        existsCmd.Parameters.AddWithValue("@ProductId", productId);
                        int stock = 0;
                        using (var reader = existsCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Response.StatusCode = 404;
                                return Json(new { ok = false, message = "Product not found or unavailable." });
                            }
                            stock = (int)reader["Stock"];
                        }
                        if (stock <= 0)
                        {
                            Response.StatusCode = 400;
                            return Json(new { ok = false, message = "Product is out of stock." });
                        }
                        if (stock < quantity)
                        {
                            Response.StatusCode = 400;
                            return Json(new { ok = false, message = "Not enough stock." });
                        }
                    }

                    var cartId = EnsureCart(userId, conn);

                    using (var upsert = conn.CreateCommand())
                    {
                        upsert.CommandText = @"
IF EXISTS (SELECT 1 FROM dbo.CartItems WHERE CartId = @CartId AND ProductId = @ProductId)
BEGIN
    UPDATE dbo.CartItems
    SET Quantity = Quantity + @Qty
    WHERE CartId = @CartId AND ProductId = @ProductId;
END
ELSE
BEGIN
    INSERT INTO dbo.CartItems (CartId, ProductId, Quantity, AddedAt)
    VALUES (@CartId, @ProductId, @Qty, GETDATE());
END";
                        upsert.Parameters.AddWithValue("@CartId", cartId);
                        upsert.Parameters.AddWithValue("@ProductId", productId);
                        upsert.Parameters.AddWithValue("@Qty", quantity);
                        upsert.ExecuteNonQuery();
                    }

                    using (var notif = conn.CreateCommand())
                    {
                        notif.CommandText = @"
INSERT INTO dbo.Notifications (UserId, Title, Body, Type, IsRead)
VALUES (@UserId, 'Cart Update', 'Item added to cart.', 'Message', 0);";
                        notif.Parameters.AddWithValue("@UserId", userId);
                        notif.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException)
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, message = "Could not add product to cart." });
            }

            return Json(new { ok = true, message = "Added to cart." });
        }

        [HttpGet]
        public JsonResult CartCount()
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                return Json(new { ok = true, count = 0 }, JsonRequestBehavior.AllowGet);
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT ISNULL(SUM(ci.Quantity), 0)
FROM dbo.Carts c
LEFT JOIN dbo.CartItems ci ON ci.CartId = c.CartId
WHERE c.UserId = @UserId;";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                return Json(new { ok = true, count }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult Catalog()
        {
            var items = new List<ProductCard>();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT ProductId, Name, ISNULL(Medium,'') AS Medium, Price, Stock, IsFlashSale, IsAvailable, ISNULL(ImagePath,'') AS ImagePath
FROM dbo.Products
WHERE IsAvailable = 1
ORDER BY ProductId DESC;";
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ProductCard
                        {
                            ProductId = (int)reader["ProductId"],
                            Name = (string)reader["Name"],
                            Medium = (string)reader["Medium"],
                            Price = (decimal)reader["Price"],
                            Stock = (int)reader["Stock"],
                            IsFlashSale = (bool)reader["IsFlashSale"],
                            IsAvailable = (bool)reader["IsAvailable"],
                            ImagePath = string.IsNullOrWhiteSpace(reader["ImagePath"] as string) ? "/Content/images/artzada_logo.png" : (string)reader["ImagePath"]
                        });
                    }
                }
            }

            var hotSales = items.Take(8).ToList();
            var flashSales = items.Where(i => i.IsFlashSale).Take(6).ToList();

            return Json(new
            {
                ok = true,
                hotSales = hotSales.Select(i => new
                {
                    id = i.ProductId,
                    name = i.Name,
                    medium = i.Medium,
                    price = i.Price,
                    stock = i.Stock,
                    isAvailable = i.IsAvailable,
                    salePrice = i.IsFlashSale ? Math.Round(i.Price * 0.5m, 2) : i.Price,
                    isFlashSale = i.IsFlashSale,
                    image = i.ImagePath
                }),
                flashSales = flashSales.Select(i => new
                {
                    id = i.ProductId,
                    name = i.Name,
                    medium = i.Medium,
                    price = i.Price,
                    stock = i.Stock,
                    isAvailable = i.IsAvailable,
                    salePrice = Math.Round(i.Price * 0.5m, 2),
                    isFlashSale = true,
                    image = i.ImagePath
                })
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetNotifications()
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                return Json(new { ok = true, items = new object[0] }, JsonRequestBehavior.AllowGet);
            }

            var items = new List<object>();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT TOP 50 NotificationId, Title, Body, Type, IsRead, CreatedAt
FROM dbo.Notifications
WHERE UserId = @UserId
ORDER BY NotificationId DESC;";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            notificationId = (int)reader["NotificationId"],
                            text = ((reader["Title"] as string) ?? "Notification") + ": " + ((reader["Body"] as string) ?? string.Empty),
                            type = MapNotificationType((reader["Type"] as string) ?? "Message"),
                            isRead = (bool)reader["IsRead"],
                            createdAt = ((DateTime)reader["CreatedAt"]).ToString("s")
                        });
                    }
                }
            }

            return Json(new { ok = true, items }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult MarkNotificationRead(int notificationId)
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
UPDATE dbo.Notifications
SET IsRead = 1
WHERE NotificationId = @NotificationId AND UserId = @UserId;";
                cmd.Parameters.AddWithValue("@NotificationId", notificationId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                var affected = cmd.ExecuteNonQuery();
                return Json(new { ok = affected > 0 });
            }
        }

        private string MapNotificationType(string type)
        {
            if (string.Equals(type, "LowStock", StringComparison.OrdinalIgnoreCase))
            {
                return "warning";
            }

            if (string.Equals(type, "Purchase", StringComparison.OrdinalIgnoreCase)
                || string.Equals(type, "Sale", StringComparison.OrdinalIgnoreCase))
            {
                return "success";
            }

            return "message";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RemoveCartItem(int productId)
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
DELETE ci
FROM dbo.CartItems ci
JOIN dbo.Carts c ON c.CartId = ci.CartId
WHERE c.UserId = @UserId AND ci.ProductId = @ProductId;";
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ProductId", productId);
                conn.Open();
                var affected = cmd.ExecuteNonQuery();
                return Json(new { ok = true, removed = affected });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateCartQuantity(int productId, int delta)
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            if (delta != 1 && delta != -1)
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, message = "Invalid quantity update." });
            }

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                int currentQty;
                int stock;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
SELECT TOP 1 ci.Quantity, p.Stock
FROM dbo.CartItems ci
JOIN dbo.Carts c ON c.CartId = ci.CartId
JOIN dbo.Products p ON p.ProductId = ci.ProductId
WHERE c.UserId = @UserId AND ci.ProductId = @ProductId;";
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            Response.StatusCode = 404;
                            return Json(new { ok = false, message = "Cart item not found." });
                        }
                        currentQty = (int)reader["Quantity"];
                        stock = (int)reader["Stock"];
                    }
                }

                var nextQty = currentQty + delta;
                if (nextQty < 1)
                {
                    nextQty = 1;
                }

                if (nextQty > stock)
                {
                    Response.StatusCode = 400;
                    return Json(new { ok = false, message = "Not enough stock." });
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
UPDATE ci
SET Quantity = @Quantity
FROM dbo.CartItems ci
JOIN dbo.Carts c ON c.CartId = ci.CartId
WHERE c.UserId = @UserId AND ci.ProductId = @ProductId;";
                    cmd.Parameters.AddWithValue("@Quantity", nextQty);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    cmd.ExecuteNonQuery();
                }
            }

            return Json(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ClearCart()
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "Unauthorized." });
            }

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
DELETE ci
FROM dbo.CartItems ci
JOIN dbo.Carts c ON c.CartId = ci.CartId
WHERE c.UserId = @UserId;";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                var affected = cmd.ExecuteNonQuery();
                return Json(new { ok = true, removed = affected });
            }
        }

        [HttpGet]
        public JsonResult GetCart()
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, items = new object[0], total = 0m }, JsonRequestBehavior.AllowGet);
            }

            var rows = new List<CartRow>();
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT p.ProductId, p.Name, p.Price, ci.Quantity, u.Username AS ArtistName, ISNULL(p.ImagePath, '') AS ImagePath
FROM dbo.Carts c
JOIN dbo.CartItems ci ON ci.CartId = c.CartId
JOIN dbo.Products p ON p.ProductId = ci.ProductId
JOIN dbo.Users u ON u.UserId = p.SellerId
WHERE c.UserId = @UserId
ORDER BY ci.AddedAt DESC;";
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new CartRow
                        {
                            ProductId = (int)reader["ProductId"],
                            Name = (string)reader["Name"],
                            ArtistName = (string)reader["ArtistName"],
                            ImagePath = string.IsNullOrWhiteSpace(reader["ImagePath"] as string) ? "/Content/images/artzada_logo.png" : (string)reader["ImagePath"],
                            Price = (decimal)reader["Price"],
                            Quantity = (int)reader["Quantity"]
                        });
                    }
                }
            }

            var total = rows.Sum(r => r.Price * r.Quantity);
            return Json(new
            {
                ok = true,
                items = rows.Select(r => new
                {
                    productId = r.ProductId,
                    name = r.Name,
                    artistName = r.ArtistName,
                    image = r.ImagePath,
                    price = r.Price,
                    quantity = r.Quantity,
                    lineTotal = r.Price * r.Quantity
                }),
                total
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PlaceOrder(string fullName, string address, string paymentMethod)
        {
            var userId = CurrentUserId();
            if (userId <= 0)
            {
                Response.StatusCode = 401;
                return Json(new { ok = false, message = "You must be logged in." });
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(address))
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, message = "Full name and address are required." });
            }

            var pay = (paymentMethod ?? "COD").Trim().Equals("GCash", StringComparison.OrdinalIgnoreCase) ? "GCash" : "COD";

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int cartId;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "SELECT TOP 1 CartId FROM dbo.Carts WHERE UserId = @UserId;";
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            var c = cmd.ExecuteScalar();
                            if (!(c is int))
                            {
                                tx.Rollback();
                                Response.StatusCode = 400;
                                return Json(new { ok = false, message = "Cart is empty." });
                            }
                            cartId = (int)c;
                        }

                        var items = new List<CartRow>();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
SELECT p.ProductId, p.Name, p.Price, ci.Quantity
FROM dbo.CartItems ci
JOIN dbo.Products p ON p.ProductId = ci.ProductId
WHERE ci.CartId = @CartId;";
                            cmd.Parameters.AddWithValue("@CartId", cartId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    items.Add(new CartRow
                                    {
                                        ProductId = (int)reader["ProductId"],
                                        Name = (string)reader["Name"],
                                        Price = (decimal)reader["Price"],
                                        Quantity = (int)reader["Quantity"]
                                    });
                                }
                            }
                        }

                        if (items.Count == 0)
                        {
                            tx.Rollback();
                            Response.StatusCode = 400;
                            return Json(new { ok = false, message = "Cart is empty." });
                        }

                        var total = items.Sum(i => i.Price * i.Quantity);
                        int orderId;
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"
INSERT INTO dbo.Orders (BuyerId, FullName, Address, ContactNumber, PaymentMethod, TotalAmount, Status, CreatedAt)
VALUES (@BuyerId, @FullName, @Address, @ContactNumber, @PaymentMethod, @TotalAmount, 'ToPay', GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                            cmd.Parameters.AddWithValue("@BuyerId", userId);
                            cmd.Parameters.AddWithValue("@FullName", fullName.Trim());
                            cmd.Parameters.AddWithValue("@Address", address.Trim());
                            cmd.Parameters.AddWithValue("@ContactNumber", "N/A");
                            cmd.Parameters.AddWithValue("@PaymentMethod", pay);
                            cmd.Parameters.AddWithValue("@TotalAmount", total);
                            orderId = (int)cmd.ExecuteScalar();
                        }

                        foreach (var item in items)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = @"
INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity, UnitPrice)
VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);";
                                cmd.Parameters.AddWithValue("@OrderId", orderId);
                                cmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                cmd.Parameters.AddWithValue("@UnitPrice", item.Price);
                                cmd.ExecuteNonQuery();
                            }

                            using (var notif = conn.CreateCommand())
                            {
                                notif.Transaction = tx;
                                notif.CommandText = @"
INSERT INTO dbo.Notifications (UserId, Title, Body, Type, IsRead)
VALUES (@UserId, 'Order Confirmed', @Message, 'Purchase', 0);";
                                notif.Parameters.AddWithValue("@UserId", userId);
                                notif.Parameters.AddWithValue("@Message", "Order placed: " + item.Name + " x" + item.Quantity);
                                notif.ExecuteNonQuery();
                            }
                        }

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = "DELETE FROM dbo.CartItems WHERE CartId = @CartId;";
                            cmd.Parameters.AddWithValue("@CartId", cartId);
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return Json(new { ok = true, message = "Payment successful.", orderId });
                    }
                    catch
                    {
                        tx.Rollback();
                        Response.StatusCode = 500;
                        return Json(new { ok = false, message = "Failed to place order." });
                    }
                }
            }
        }
    }
}
