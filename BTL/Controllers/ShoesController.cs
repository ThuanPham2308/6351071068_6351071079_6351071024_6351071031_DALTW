using BTL.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTL.Controllers
{
    public class ShoesController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        public ActionResult Index(int? page, int? orderby)
        {
            var typeCategory = menShopEntities.TypeCategories
                                               .FirstOrDefault(c => c.TypeCategoryName.Contains("Giày dép"));
            if (typeCategory == null)
            {
                return HttpNotFound("Không tìm thấy TypeCategory chứa 'Giày dép'");
            }

            var products = menShopEntities.Products
                                          .Where(p => p.Category.TypeCategoryID == typeCategory.TypeCategoryID)
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

            products = SortProducts(products, orderby, discountedProductIDs, discountPercentage);

            int pageSize = 10;
            int pageNumber = page ?? 1;

            ViewData["CategoryName"] = typeCategory.TypeCategoryName;
            ViewData["CategoryID"] = typeCategory.TypeCategoryID;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["orderby"] = orderby;

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
        public ActionResult ShowShoesByIDCategory(int id, int? page, int? orderby)
        {
            var products = menShopEntities.Products.Where(p => p.CategoryID == id).ToList();

            var discountProductList = menShopEntities.DiscountPrograms
                                                     .Where(dp => dp.StartDate <= DateTime.Now && dp.EndDate >= DateTime.Now)
                                                     .OrderByDescending(dp => dp.CreatedDate)
                                                     .FirstOrDefault();

            decimal discountPercentage = 0;
            List<int> discountedProductIDs = new List<int>();

            if (discountProductList != null)
            {
                discountPercentage = discountProductList.DiscountPercentage;

                discountedProductIDs = menShopEntities.DiscountProducts
                                                      .Where(dp => dp.DiscountProgramID == discountProductList.DiscountProgramID)
                                                      .Select(dp => dp.ProductID)
                                                      .ToList();
            }

            products = SortProducts(products, orderby, discountedProductIDs, discountPercentage);

            int pageSize = 10;
            int pageNumber = page ?? 1;

            ViewData["CurrentPage"] = pageNumber;
            ViewData["CategoryID"] = id;

            return View(products.ToPagedList(pageNumber, pageSize));
        }
    }
}