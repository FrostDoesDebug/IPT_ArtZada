using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Store";
            ViewBag.Page = "store";
            return View();
        }

        public ActionResult BuyerMessage()
        {
            return View();
        }   
        public ActionResult Cart()
        {
            ViewBag.Title = "Cart";
            ViewBag.Page = "Cart";
            return View();
        }

        public ActionResult Message(string withUserId = null)
        {
            ViewBag.Title = "Messaging";
            ViewBag.Page = "message";
            ViewBag.WithUserId = withUserId;
            return View();
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

            return RedirectToAction("Message", new { withUserId = receiverId });
        }

        public ActionResult Notifications()
        {
            ViewBag.Title = "Notifications";
            ViewBag.Page = "notifications";
            return View();
        }

        public ActionResult Analytics()
        {
            ViewBag.Title = "Analytics";
            ViewBag.Page = "analytics";
            ViewBag.Revenue = "100.00";
            return View();
        }

        public ActionResult Account()
        {
            ViewBag.Title = "Account";
            ViewBag.Page = "account";
            ViewBag.Username = "PAMILI IS LOVE";
            ViewBag.IsSeller = true;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUsername(string NewUsername)
        {
            if (!string.IsNullOrWhiteSpace(NewUsername))
            {
                // TODO: update username in DB.
            }

            return RedirectToAction("Account");
        }

        public ActionResult ChangeProfile()
        {
            ViewBag.Title = "Change Profile";
            ViewBag.Page = "account";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeProfile(HttpPostedFileBase avatarFile)
        {
            if (avatarFile != null && avatarFile.ContentLength > 0)
            {
                // TODO: save avatar file and update user profile.
            }

            return RedirectToAction("Account");
        }

        public ActionResult ToggleSeller()
        {
            return RedirectToAction("Account");
        }

        public ActionResult SwitchToBuyer()
        {
            return RedirectToAction("Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SignOut()
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Product()
        {
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult SellerProf()
        {
            return View();
        }

        public ActionResult AdminAccount()
        {
            ViewBag.AdminUsername = "";
            ViewBag.ProfileImageUrl = "";
            return View();
        }

        public ActionResult AdminEditAccount()
        {
            return View();
        }

        public ActionResult SellerSideStore()
        {
            return View();
        }

        public ActionResult SellerSideStoreAddItem()
        {
            return View();
        }

        public ActionResult SellerSideEditItem()
        {
            return View();
        }
        
    }
}