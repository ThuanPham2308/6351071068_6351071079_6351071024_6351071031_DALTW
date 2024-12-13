using BTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

using System.Web.Mvc;

namespace BTL.Controllers
{
    public class ProductController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        // GET: Product
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Details(int id)
        {
            var product = menShopEntities.Products
                .Include(p => p.Category)
                .Include(p => p.Category.TypeCategory)
                .Include(p => p.ProductPictures)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return HttpNotFound();
            }

            var discountProgram = menShopEntities.DiscountPrograms
                .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                .OrderByDescending(dp => dp.CreatedDate)
                .FirstOrDefault();

            decimal finalPrice = product.Price;
            decimal discountPercentage = 0;

            if (discountProgram != null)
            {
                var discountedProductIDs = menShopEntities.DiscountProducts
                    .Where(dp => dp.DiscountProgramID == discountProgram.DiscountProgramID)
                    .Select(dp => dp.ProductID)
                    .ToList();

                if (discountedProductIDs.Contains(product.ProductID))
                {
                    discountPercentage = discountProgram.DiscountPercentage;
                    finalPrice = product.Price - (product.Price * discountPercentage / 100);
                }
            }

            ViewData["FinalPrice"] = finalPrice;
            ViewData["DiscountPercentage"] = discountPercentage;

            return View(product);
        }

        public ActionResult AnotherProducts(int id)
        {
            var product = menShopEntities.Products.FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                return View("Error");
            }

            var categoryId = product.CategoryID;

            var otherProducts = menShopEntities.Products
                                    .Where(p => p.CategoryID == categoryId && p.ProductID != id)
                                    .Take(10)
                                    .ToList();

            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            List<int> discountedProductIDs = new List<int>();
            Dictionary<int, decimal> finalPrices = new Dictionary<int, decimal>();

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;

                var discountProgramID = discountProductList.DiscountProgramID;
                discountedProductIDs = menShopEntities.DiscountProducts
                                                      .Where(dp => dp.DiscountProgramID == discountProgramID)
                                                      .Select(dp => dp.ProductID)
                                                      .ToList();

                finalPrices = otherProducts.ToDictionary(p => p.ProductID,
                                                         p => discountedProductIDs.Contains(p.ProductID)
                                                             ? p.Price - (p.Price * discountPercentage / 100)
                                                             : p.Price);
            }

            ViewData["OtherProducts"] = otherProducts;
            ViewData["FinalPrices"] = finalPrices;

            return PartialView(otherProducts.ToList());
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
                .Take(5) 
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
            Dictionary<int, decimal> finalPrices = new Dictionary<int, decimal>();

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;

                var discountProgramID = discountProductList.DiscountProgramID;
                discountedProductIDs = menShopEntities.DiscountProducts
                                                      .Where(dp => dp.DiscountProgramID == discountProgramID)
                                                      .Select(dp => dp.ProductID)
                                                      .ToList();

                finalPrices = productDetails.ToDictionary(p => p.ProductID,
                                                         p => discountedProductIDs.Contains(p.ProductID)
                                                             ? p.Price - (p.Price * discountPercentage / 100)
                                                             : p.Price);
            }

            ViewData["OtherProducts"] = productDetails;
            ViewData["FinalPrices"] = finalPrices;

            return PartialView(productDetails.ToList());
        }

    }
}