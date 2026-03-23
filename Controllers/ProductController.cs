using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ArtZada.Controllers
{
    public class ProductController : Controller
    {
        // Action to show product details
        public ActionResult Details(int id)
        {
            // TODO: Load the product data based on id
            ViewBag.ProductId = id;
            return View();
        }
    }
}