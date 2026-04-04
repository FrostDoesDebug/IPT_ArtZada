using System.Web;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class HomeController : Controller
    {
        private const string UserNameSessionKey = "UserName";
        private const string UserRoleSessionKey = "UserRole";
        private const string ClientRole = "Client";
        private const string SellerRole = "Seller";
        private const string AdminRole = "Admin";

        private bool IsLoggedIn()
        {
            return !string.IsNullOrWhiteSpace(Session[UserNameSessionKey] as string);
        }

        private string CurrentRole()
        {
            return (Session[UserRoleSessionKey] as string ?? ClientRole).Trim();
        }

        private bool IsSellerRole()
        {
            return string.Equals(CurrentRole(), SellerRole, System.StringComparison.OrdinalIgnoreCase);
        }

        private bool IsAdminRole()
        {
            return string.Equals(CurrentRole(), AdminRole, System.StringComparison.OrdinalIgnoreCase);
        }

        private ActionResult RedirectToRoleHome()
        {
            if (IsAdminRole())
            {
                return RedirectToAction("UserOverview", "Admin");
            }

            if (IsSellerRole())
            {
                return RedirectToAction("Store", "Seller");
            }

            return RedirectToAction("Index", "Client");
        }

        public ActionResult Index()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Login");
            }

            return RedirectToRoleHome();
        }

        public ActionResult BuyerMessage()
        {
            return RedirectToAction("Message", "Client");
        }   

        public ActionResult Cart()
        {
            return RedirectToAction("Cart", "Client");
        }

        public ActionResult Message(string withUserId = null)
        {
            if (IsSellerRole())
            {
                return RedirectToAction("Message", "Seller", new { withUserId });
            }

            return RedirectToAction("Message", "Client", new { withUserId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendMessage(string receiverId, string messageText)
        {
            if (!string.IsNullOrWhiteSpace(messageText)
                && !string.IsNullOrWhiteSpace(receiverId))
            {
                // TODO: persist chat messages to DB.
            }

            if (IsSellerRole())
            {
                return RedirectToAction("Message", "Seller", new { withUserId = receiverId });
            }

            return RedirectToAction("Message", "Client", new { withUserId = receiverId });
        }

        public ActionResult Notifications()
        {
            if (IsSellerRole())
            {
                return RedirectToAction("Notifications", "Seller");
            }

            return RedirectToAction("Notifications", "Client");
        }

        public ActionResult Analytics()
        {
            if (IsSellerRole())
            {
                return RedirectToAction("Analytics", "Seller");
            }

            return RedirectToAction("Analytics", "Client");
        }

        public ActionResult Account()
        {
            if (IsSellerRole())
            {
                return RedirectToAction("Account", "Seller");
            }

            return RedirectToAction("Account", "Client");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUsername(string NewUsername)
        {
            if (!string.IsNullOrWhiteSpace(NewUsername))
            {
                // TODO: update username in DB.
            }

            if (IsSellerRole())
            {
                return RedirectToAction("Account", "Seller");
            }

            return RedirectToAction("Account", "Client");
        }

        public ActionResult ChangeProfile()
        {
            return RedirectToAction("Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeProfile(HttpPostedFileBase avatarFile)
        {
            if (avatarFile != null && avatarFile.ContentLength > 0)
            {
                // TODO: save avatar file and update user profile.
            }

            if (IsSellerRole())
            {
                return RedirectToAction("Account", "Seller");
            }

            return RedirectToAction("Account", "Client");
        }

        public ActionResult ToggleSeller()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Login");
            }

            Session[UserRoleSessionKey] = SellerRole;
            return RedirectToAction("Store", "Seller");
        }

        public ActionResult SwitchToBuyer()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Login");
            }

            Session[UserRoleSessionKey] = ClientRole;
            return RedirectToAction("Index", "Client");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignOut()
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Landing", "Landing");
        }

        public ActionResult Product()
        {
            return RedirectToAction("Product", "Client");
        }

        public ActionResult Contact()
        {
            return RedirectToAction("Landing", "Landing");
        }

        public ActionResult About()
        {
            return RedirectToAction("Landing", "Landing");
        }

        public ActionResult SellerProf()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Login");
            }

            return View();
        }

        public ActionResult AdminAccount()
        {
            return RedirectToAction("Account", "Admin");
        }

        public ActionResult AdminEditAccount()
        {
            return RedirectToAction("Account", "Admin");
        }

        public ActionResult SellerSideStore()
        {
            return RedirectToAction("Store", "Seller");
        }

        public ActionResult SellerSideStoreAddItem()
        {
            return RedirectToAction("AddItem", "Seller");
        }

        public ActionResult SellerSideEditItem()
        {
            return RedirectToAction("EditItem", "Seller");
        }
        
    }
}