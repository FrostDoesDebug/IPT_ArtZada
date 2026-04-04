using System;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class ClientController : Controller
    {
        private const string UserNameSessionKey = "UserName";
        private const string UserRoleSessionKey = "UserRole";
        private const string ClientRole = "Client";
        private const string SellerRole = "Seller";
        private const string AdminRole = "Admin";

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

        public ActionResult Product()
        {
            return View();
        }

        public ActionResult Cart()
        {
            return View();
        }
    }
}
