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
            return View();
        }

        public ActionResult Message()
        {
            return View();
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
        public ActionResult SellerProf()
        {
            return View();
        }
        public ActionResult AdminAccount()
        {
            ViewBag.AdminUsername = ""; // Replace with your session/db value
            ViewBag.ProfileImageUrl = "";        // Replace with actual image path if available
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