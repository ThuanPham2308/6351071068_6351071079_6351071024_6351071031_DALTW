using BTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Web.Mvc;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using PagedList;

namespace BTL.Controllers
{
    public class HomeController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        public ActionResult index()
        {
            return View();
        }
        //public ActionResult myAcount()
        //{
        //    return View();
        //}

        public ActionResult contact()
        {
            var customer = Session["Customer"] as Customer;
            if(customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        public ActionResult contact(string topic, string message)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Contact contact = new Contact
            {
                CustomerID = customer.CustomerID,
                AdminID = 1,
                Topic = topic,
                Message = message,
                CreatedDate = DateTime.Now
            };

            menShopEntities.Contacts.Add(contact);
            menShopEntities.SaveChanges();

            SendEmail(customer.Email);
            return View();
        }

        private void SendEmail(string customerEmail)
        {
            var fromAddress = new MailAddress("quangvinhdang7a1@gmail.com", "Your Website");
            var toAddress = new MailAddress(customerEmail);
            const string subject = "Thông báo về ý kiến phản hồi";
            string body = $"Kính gửi quý khách,\n\n Cảm ơn về phản hồi của bạn. Shop sẽ khắc phục sớm!\n\nTrân trọng";

            var username = ConfigurationManager.AppSettings["EmailUsername"];
            var appPassword = ConfigurationManager.AppSettings["EmailPassword"];
            var smtpHost = "smtp.gmail.com";
            var smtpPort = 587;

            using (var smtp = new SmtpClient(smtpHost, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(username, appPassword);
                smtp.EnableSsl = true;
                smtp.Send(fromAddress.Address, toAddress.Address, subject, body);
            }
        }


        public ActionResult Error()
        {
            return View();
        }

        //public List<Product> NewProductsList(int count)
        //{
        //    var products = menShopEntities.Products
        //               .Include(p => p.Category)
        //               .Include(p => p.Category.TypeCategory)
        //               .Where(p => p.Category != null && p.Category.TypeCategory != null)
        //               .OrderByDescending(p => p.CreatedDate) // Sắp xếp theo thời gian cập nhật gần nhất
        //               .Take(count) // Lấy số lượng sản phẩm giới hạn (10 sản phẩm)
        //               .ToList();

        //    return products;
        //}

        
        public ActionResult NewProductsList()
        {
            var products = menShopEntities.Products
                .OrderByDescending(p => p.CreatedDate) 
                .ThenByDescending(p => p.ProductID)  
                .Take(10)  
                .ToList();

            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            List<int> discountedProductIDs = new List<int>();

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;
                Session["DiscountPercentage"] = discountPercentage;

                var discountProgramID = discountProductList.DiscountProgramID;
                discountedProductIDs = menShopEntities.DiscountProducts
                                                          .Where(dp => dp.DiscountProgramID == discountProgramID)
                                                          .Select(dp => dp.ProductID)
                                                          .ToList();

                Session["DiscountedProductIDs"] = discountedProductIDs;

                var finalPrices = products.ToDictionary(p => p.ProductID,
                                                        p => discountedProductIDs.Contains(p.ProductID)
                                                            ? p.Price - (p.Price * discountPercentage / 100)
                                                            : p.Price
                );

                Session["FinalPrices"] = finalPrices;


            }

            return PartialView(products);
        }

        public ActionResult BestSellingProductsList()
        {
            var bestSellingProducts = menShopEntities.InvoiceDetails
                .GroupBy(id => id.ProductID) 
                .Select(group => new
                {
                    ProductID = group.Key,
                    TotalQuantitySold = group.Sum(id => id.Quantity)  
                })
                .OrderByDescending(p => p.TotalQuantitySold) 
                .Take(10)  
                .ToList();

            var productDetails = from bestSelling in bestSellingProducts
                                 join product in menShopEntities.Products
                                 on bestSelling.ProductID equals product.ProductID
                                 select new Product
                                 {
                                     ProductID = product.ProductID,
                                     ProductName = product.ProductName,
                                     Price = product.Price,
                                     URLPicture = product.URLPicture,
                                     Category = product.Category  
                                 };

            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            List<int> discountedProductIDs = new List<int>();

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;
                Session["DiscountPercentage"] = discountPercentage;

                var discountProgramID = discountProductList.DiscountProgramID;
                discountedProductIDs = menShopEntities.DiscountProducts
                                                          .Where(dp => dp.DiscountProgramID == discountProgramID)
                                                          .Select(dp => dp.ProductID)
                                                          .ToList();

                Session["DiscountedProductIDs"] = discountedProductIDs;

                var finalPrices = productDetails.ToDictionary(p => p.ProductID,
                                                        p => discountedProductIDs.Contains(p.ProductID)
                                                            ? p.Price - (p.Price * discountPercentage / 100)
                                                            : p.Price
                );

                Session["FinalPrices"] = finalPrices;


            }
            return PartialView(productDetails.ToList());
        }





        public int GetIDTypeCategoryByName(string name)
        {
            var typeCategory = menShopEntities.TypeCategories
                .FirstOrDefault(s => s.TypeCategoryName.Contains(name));

            return typeCategory != null ? typeCategory.TypeCategoryID : 0;
        }

        public ActionResult MenShirts()
        {
            int id = GetIDTypeCategoryByName("Áo nam");
            var menShirts = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == id) select ms;
            //var menShirts = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == 1) select ms;
            return PartialView(menShirts);
        }
        public ActionResult MenPants()
        {
            int id = GetIDTypeCategoryByName("Quần nam");
            var menPants = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == id) select ms;
            //var menShirts = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == 1) select ms;
            return PartialView(menPants);
        }
        public ActionResult Accessories()
        {
            int id = GetIDTypeCategoryByName("Phụ kiện");
            var accessory = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == id) select ms;
            //var menShirts = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == 1) select ms;
            return PartialView(accessory);
        }
        public ActionResult Shoes()
        {
            int id = GetIDTypeCategoryByName("Giày dép");
            var shoe = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == id) select ms;
            //var menShirts = from ms in menShopEntities.Categories.Where(s => s.TypeCategoryID == 1) select ms;
            return PartialView(shoe);
        }

        public ActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Search(string search)
        {
            var products = menShopEntities.Products
                .Where(p => p.ProductName.Contains(search))
                .Select(p => new { p.ProductID, p.ProductName })
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ProductSale()
        {
            var discountProductList = menShopEntities.DiscountPrograms
                                                   .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                   .OrderByDescending(dp => dp.CreatedDate)
                                                   .FirstOrDefault();

            decimal discountPercentage = 0;

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;

                Session["DiscountPercentage"] = discountPercentage;

                var discountProgramID = discountProductList.DiscountProgramID;

                ViewBag.DiscountProgramName = discountProductList.DiscountProgramName;

                var discountedProducts = menShopEntities.DiscountProducts
                                                        .Where(dp => dp.DiscountProgramID == discountProgramID)
                                                        .Select(dp => dp.Product)
                                                        .ToList();

                var discountedProductIDs = discountedProducts.Select(p => p.ProductID).ToList();
                Session["DiscountedProductIDs"] = discountedProductIDs;

                var finalPrices = discountedProducts.ToDictionary(
                    p => p.ProductID,
                    p => p.Price - (p.Price * discountPercentage / 100)
                );
                Session["FinalPrices"] = finalPrices;

                return PartialView(discountedProducts);
            }

            return View(new List<Product>().ToPagedList(1, 2));
        }


        public ActionResult SearchProduct(string search)
        {
            var products = new List<Product>();

            if (!string.IsNullOrEmpty(search))
            {
                products = menShopEntities.Products.Where(p => p.ProductName.Contains(search)).ToList();
            }
            else
            {
                var referrerUrl = Request.UrlReferrer?.AbsolutePath ?? Url.Action("SearchProduct", "Home");
                return Redirect(referrerUrl);
            }

            ViewData["Search"] = search;

            return View(products);
        }

        [HttpPost]
        public ActionResult SearchProduct(string search, int? page, int? orderby)
        {
            var products = new List<Product>();

            if (!string.IsNullOrEmpty(search))
            {
                products = menShopEntities.Products.Where(p => p.ProductName.Contains(search)).ToList();
            }
            else
            {
                var referrerUrl = Request.UrlReferrer?.AbsolutePath ?? Url.Action("SearchProduct", "Home");
                return Redirect(referrerUrl);
            }

            // Tính toán giá trị giảm giá
            var discountProductList = menShopEntities.DiscountPrograms
                                                    .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                    .OrderByDescending(dp => dp.CreatedDate)
                                                    .FirstOrDefault();

            decimal discountPercentage = 0;
            List<int> discountedProductIDs = new List<int>();

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;
                Session["DiscountPercentage"] = discountPercentage;

                var discountProgramID = discountProductList.DiscountProgramID;
                discountedProductIDs = menShopEntities.DiscountProducts
                                                          .Where(dp => dp.DiscountProgramID == discountProgramID)
                                                          .Select(dp => dp.ProductID)
                                                          .ToList();

                Session["DiscountedProductIDs"] = discountedProductIDs;

                var finalPrices = products.ToDictionary(p => p.ProductID,
                                                        p => discountedProductIDs.Contains(p.ProductID)
                                                            ? p.Price - (p.Price * discountPercentage / 100)
                                                            : p.Price
                );

                Session["FinalPrices"] = finalPrices;
            }

            products = SortProducts(products, orderby, discountedProductIDs, discountPercentage);

            int pageSize = 20;
            int pageNumber = page ?? 1;

            ViewData["Search"] = search;
            ViewData["Orderby"] = orderby;
            ViewData["CurrentPage"] = pageNumber;

            return View(products.ToPagedList(pageNumber, pageSize));
        }


        private List<Product> SortProducts(List<Product> products, int? orderby, List<int> discountedProductIDs, decimal discountPercentage)
        {
            var productsWithFinalPrice = products.Select(p => new
            {
                Product = p,
                FinalPrice = discountedProductIDs.Contains(p.ProductID)
                    ? p.Price - (p.Price * discountPercentage / 100)
                    : p.Price
            }).ToList();

            switch (orderby)
            {
                case 1:
                    return productsWithFinalPrice.OrderBy(p => p.Product.ProductName).Select(p => p.Product).ToList();
                case 2:
                    return productsWithFinalPrice.OrderByDescending(p => p.Product.ProductName).Select(p => p.Product).ToList();
                case 3:
                    return productsWithFinalPrice.OrderByDescending(p => p.FinalPrice).Select(p => p.Product).ToList();
                case 4:
                    return productsWithFinalPrice.OrderBy(p => p.FinalPrice).Select(p => p.Product).ToList();
                default:
                    return productsWithFinalPrice.OrderBy(p => p.Product.ProductName).Select(p => p.Product).ToList();
            }
        }
    }
}