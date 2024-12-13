using BTL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTL.Controllers
{
    public class CartController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        // GET: Cart
        public ActionResult Index()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = menShopEntities.Carts
                                           .Where(w => w.CustomerID == customer.CustomerID)
                                           .Include(w => w.Product)
                                           .ToList();

            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;
            }

            decimal totalAmount = 0;
            List<int> discountedProductList = new List<int>();

            foreach (var item in cartItems)
            {
                decimal productPrice = item.Product.Price;

                bool isDiscounted = menShopEntities.DiscountProducts
                                                  .Any(dp => dp.DiscountProgramID == discountProductList.DiscountProgramID
                                                             && dp.ProductID == item.ProductID);

                if (isDiscounted)
                {
                    productPrice -= (productPrice * discountPercentage / 100);
                    discountedProductList.Add(item.ProductID);
                }

                totalAmount += productPrice * item.Quantity ?? 0;
            }

            string formattedAmount = totalAmount.ToString("#,0");
            ViewData["TotalAmount"] = formattedAmount;
            ViewData["DiscountProductList"] = discountedProductList;
            ViewData["DiscountPercentage"] = discountPercentage;

            return View(cartItems);
        }





        public ActionResult AddToCart(int id)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = menShopEntities.Products.SingleOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return HttpNotFound("Product not found");
            }

            var existingCartItem = menShopEntities.Carts
                .FirstOrDefault(c => c.CustomerID == customer.CustomerID && c.ProductID == id);

            if (existingCartItem == null)
            {
                var newCartItem = new Cart
                {
                    CustomerID = customer.CustomerID,
                    ProductID = id,
                    Quantity = 1
                };

                menShopEntities.Carts.Add(newCartItem);
            }
            else
            {
                existingCartItem.Quantity++;
            }

            menShopEntities.SaveChanges();

            var referrerUrl = Request.UrlReferrer?.AbsolutePath ?? Url.Action("Index", "Cart");
            return Redirect(referrerUrl);
        }

        public ActionResult RemoveFromCart(int id, bool fromMiniCart = false)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = menShopEntities.Carts.SingleOrDefault(c => c.CustomerID == customer.CustomerID && c.CartID == id);

            if (cartItem != null)
            {
                menShopEntities.Carts.Remove(cartItem);
                menShopEntities.SaveChanges();
            }

            if (fromMiniCart)
            {
                var referrerUrl = Request.UrlReferrer?.AbsolutePath ?? Url.Action("Index", "Cart");
                return Redirect(referrerUrl);
            }

            return RedirectToAction("Index", "Cart");
        }


        public ActionResult UpdateCart()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = menShopEntities.Carts.Where(c => c.CustomerID == customer.CustomerID).ToList();
            return View(cartItems); 
        }

        [HttpPost]
        public ActionResult UpdateCart(int id, int quantity)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = menShopEntities.Carts.SingleOrDefault(c => c.CustomerID == customer.CustomerID && c.CartID == id);

            if (cartItem != null)
            {
                var product = menShopEntities.Products.SingleOrDefault(p => p.ProductID == cartItem.ProductID);

                if (product != null)
                {
                    cartItem.Quantity = quantity;
                    menShopEntities.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Cart");
        }

        public ActionResult ConfirmPayment()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var latestInvoice = menShopEntities.Invoices
                                               .Where(i => i.CustomerID == customer.CustomerID)
                                               .OrderByDescending(i => i.CreatedDate)
                                               .FirstOrDefault();

            if (latestInvoice == null)
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Invoice = latestInvoice;
            ViewBag.InvoiceDetails = menShopEntities.InvoiceDetails
                                                    .Where(d => d.InvoiceID == latestInvoice.InvoiceID)
                                                    .ToList();

            
            return View();
        }


        public ActionResult PayMoney()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Retrieve the cart items for the customer
            var cartItems = menShopEntities.Carts
                                           .Where(c => c.CustomerID == customer.CustomerID)
                                           .Include(c => c.Product)
                                           .ToList();

            if (cartItems.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // Get the current active discount program
            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;
            }

            // Calculate the total amount, considering discounts
            decimal totalAmount = 0;
            List<int> discountedProductList = new List<int>();

            foreach (var item in cartItems)
            {
                decimal productPrice = item.Product.Price;

                bool isDiscounted = false;

                if (discountProductList != null)
                {
                    isDiscounted = menShopEntities.DiscountProducts
                                                  .Any(dp => dp.DiscountProgramID == discountProductList.DiscountProgramID
                                                             && dp.ProductID == item.ProductID);
                }

                if (isDiscounted)
                {
                    productPrice -= (productPrice * discountPercentage / 100);
                    discountedProductList.Add(item.ProductID);
                }

                totalAmount += productPrice * item.Quantity ?? 0;
            }

            string formattedAmount = totalAmount.ToString("#,0");

            var invoice = new Invoice
            {
                CustomerID = customer.CustomerID,
                CreatedDate = DateTime.Now,
                Status = "Đang chờ xác nhận đơn",
                TotalAmount = totalAmount
            };

            menShopEntities.Invoices.Add(invoice);
            menShopEntities.SaveChanges();

            foreach (var cartItem in cartItems)
            {
                decimal productPrice = cartItem.Product.Price;

                bool isDiscounted = false;
                if (discountProductList != null)
                {
                    isDiscounted = menShopEntities.DiscountProducts
                                                  .Any(dp => dp.DiscountProgramID == discountProductList.DiscountProgramID
                                                             && dp.ProductID == cartItem.ProductID);
                }

                if (isDiscounted)
                {
                    productPrice -= (productPrice * discountPercentage / 100);
                }

                var invoiceDetail = new InvoiceDetail
                {
                    InvoiceID = invoice.InvoiceID,
                    ProductID = cartItem.ProductID,
                    Quantity = cartItem.Quantity ?? 1,
                    Price = productPrice 
                };

                menShopEntities.InvoiceDetails.Add(invoiceDetail);
            }

            menShopEntities.SaveChanges();

            foreach (var cartItem in cartItems)
            {
                menShopEntities.Carts.Remove(cartItem);
            }

            menShopEntities.SaveChanges();

            ViewBag.TotalAmount = formattedAmount;

            ViewData["DiscountProductList"] = discountedProductList;
            ViewData["DiscountPercentage"] = discountPercentage;

            return View();
        }


        public ActionResult MiniCart()
        {
            var customer = Session["Customer"] as Customer;

            var cartItems = menShopEntities.Carts
                                           .Where(c => c.CustomerID == customer.CustomerID)
                                           .Include(c => c.Product)
                                           .ToList();

            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;
            }

            decimal totalAmount = 0;
            List<int> discountedProductList = new List<int>();

            foreach (var item in cartItems)
            {
                decimal productPrice = item.Product.Price;

                bool isDiscounted = menShopEntities.DiscountProducts
                                                  .Any(dp => dp.DiscountProgramID == discountProductList.DiscountProgramID
                                                             && dp.ProductID == item.ProductID);

                if (isDiscounted)
                {
                    productPrice -= (productPrice * discountPercentage / 100);
                    discountedProductList.Add(item.ProductID);
                }

                totalAmount += productPrice * (item.Quantity ?? 0);
            }

            string formattedAmount = totalAmount.ToString("#,0");
            ViewData["TotalAmount"] = formattedAmount;
            ViewData["DiscountProductList"] = discountedProductList;
            ViewData["DiscountPercentage"] = discountPercentage;

            return PartialView(cartItems);
        }


    }
}