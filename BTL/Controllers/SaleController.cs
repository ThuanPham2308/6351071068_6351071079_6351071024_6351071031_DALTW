using BTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;

namespace BTL.Controllers
{
    public class SaleController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();

        // GET: Sale
        public ActionResult Index(int? page, int? orderby)
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
            }

            if (discountProductList == null)
            {
                return View(new List<Product>().ToPagedList(1, 2));
            }

            var discountProgramID = discountProductList.DiscountProgramID;

            var products = menShopEntities.DiscountProducts
                                         .Where(dp => dp.DiscountProgramID == discountProgramID)
                                         .Select(dp => dp.Product)
                                         .ToList();

            products = SortProducts(products, orderby);

            int pageSize = 2;
            int pageNumber = page ?? 1;

            ViewData["CurrentPage"] = pageNumber;
            ViewData["orderby"] = orderby;

            return View(products.ToPagedList(pageNumber, pageSize));
        }

        private List<Product> SortProducts(List<Product> products, int? orderby)
        {
            switch (orderby)
            {
                case 1:
                    return products.OrderBy(p => p.ProductName).ToList();
                case 2:
                    return products.OrderByDescending(p => p.ProductName).ToList();
                case 3:
                    return products.OrderByDescending(p => p.Price).ToList();
                case 4:
                    return products.OrderBy(p => p.Price).ToList();
                default:
                    return products.OrderBy(p => p.ProductName).ToList();
            }
        }
    }

}