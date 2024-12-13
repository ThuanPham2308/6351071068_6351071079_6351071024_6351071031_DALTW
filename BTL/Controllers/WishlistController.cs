using BTL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTL.Controllers
{
    public class WishlistController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        // GET: Wishlist
        public ActionResult Index()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItems = menShopEntities.Wishlists
                                               .Where(w => w.CustomerID == customer.CustomerID)
                                               .Include(w => w.Product)
                                               .ToList();

            return View(wishlistItems);
        }

        public ActionResult AddToWishlist(int id)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = menShopEntities.Products.SingleOrDefault(p => p.ProductID == id);

            if (product != null)
            {
                var existingWishlistItem = menShopEntities.Wishlists
                                                          .FirstOrDefault(w => w.CustomerID == customer.CustomerID && w.ProductID == id);

                if (existingWishlistItem == null)
                {
                    string status;
                    if(product.QuantityInStock > 0)
                    {
                        status = "Còn hàng";
                    }
                    else
                    {
                        status = "Hết hàng";
                    }
                    var newWishlistItem = new Wishlist
                    {
                        CustomerID = customer.CustomerID,
                        ProductID = id,
                        Status = status, 
                        CreatedDate = DateTime.Now
                    };

                    menShopEntities.Wishlists.Add(newWishlistItem);
                    menShopEntities.SaveChanges(); 
                }
            }
            var referrerUrl = Request.UrlReferrer?.AbsolutePath ?? Url.Action("Index", "Wishlist");
            return Redirect(referrerUrl);
        }

        public ActionResult RemoveFromWishlist(int id)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlistItem = menShopEntities.Wishlists
                                              .FirstOrDefault(w => w.CustomerID == customer.CustomerID && w.WishlistID == id);

            if (wishlistItem != null)
            {
                menShopEntities.Wishlists.Remove(wishlistItem);
                menShopEntities.SaveChanges();
            }

            return RedirectToAction("Index", "Wishlist");
        }

    }
}