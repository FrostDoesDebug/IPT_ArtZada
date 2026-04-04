using System;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class SellerController : Controller
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
    }
}
