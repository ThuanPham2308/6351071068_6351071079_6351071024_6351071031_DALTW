using BTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTL.Areas.AdminArea.Controllers
{
    public class RevenueReportsController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/RevenueReports
        public ActionResult index()
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var totalRevenueYear = db.Invoices
                 .Where(invoice => invoice.CreatedDate.HasValue && invoice.CreatedDate.Value.Year == DateTime.Now.Year)
                 .Sum(invoice => invoice.TotalAmount);
            ViewBag.totalRevenueYear = totalRevenueYear;
            var totalOrdersYear = db.Invoices
                          .Where(invoice => invoice.CreatedDate.HasValue && invoice.CreatedDate.Value.Year == DateTime.Now.Year)
                          .Count();
            ViewBag.totalOrdersYear = totalOrdersYear;
            var totalRevenueMonth = db.Invoices
                               .Where(invoice => invoice.CreatedDate.HasValue &&
                                              invoice.CreatedDate.Value.Month == DateTime.Now.Month &&
                                              invoice.CreatedDate.Value.Year == DateTime.Now.Year)
                               .Sum(invoice => invoice.TotalAmount);
            ViewBag.totalRevenueMonth = totalRevenueMonth;
            var totalOrdersMonth = db.Invoices
                           .Where(invoice => invoice.CreatedDate.HasValue &&
                                             invoice.CreatedDate.Value.Month == DateTime.Now.Month &&
                                             invoice.CreatedDate.Value.Year == DateTime.Now.Year)
                           .Count();
            ViewBag.totalOrdersMonth = totalOrdersMonth;

            //RevenueByGroupCtx
            ViewBag.revenueMenShirts = GetRevenueByTypeCategory("Áo nam");
            ViewBag.revenueMenPants = GetRevenueByTypeCategory("Quần nam");
            ViewBag.revenueAccessory = GetRevenueByTypeCategory("Phụ kiện");
            ViewBag.revenueMenShoes = GetRevenueByTypeCategory("Giày dép");


            var months = Enumerable.Range(1, 12).Select(m => new { Month = m, Year = 2024 });

            var revenueTC1 = from m in months
                             join i in db.Invoices on m.Month equals i.CreatedDate?.Month into invoiceGroup
                             from inv in invoiceGroup.DefaultIfEmpty()
                             where inv?.CreatedDate.HasValue == true && inv.CreatedDate.Value.Month == m.Month
                             join id in db.InvoiceDetails on inv?.InvoiceID equals id?.InvoiceID into invoiceDetailsGroup
                             from id in invoiceDetailsGroup.DefaultIfEmpty()
                             join p in db.Products on id?.ProductID equals p?.ProductID into productsGroup
                             from prod in productsGroup.DefaultIfEmpty()
                             join c in db.Categories on prod?.CategoryID equals c?.CategoryID into categoriesGroup
                             from cat in categoriesGroup.DefaultIfEmpty()
                             join tc in db.TypeCategories on cat?.TypeCategoryID equals tc?.TypeCategoryID
                             where tc?.TypeCategoryName == "Áo nam"
                             group id by new { m.Month, Year = 2024 } into g
                             select new
                             {
                                 Month = g.Key.Month,
                                 Year = g.Key.Year,
                                 Revenue = g.Sum(x => x?.Quantity * x?.Price) ?? 0 // Sử dụng toán tử ?? để gán giá trị 0 nếu không có doanh thu
                             };

            var revenueTC2 = from m in months
                             join i in db.Invoices on m.Month equals i.CreatedDate?.Month into invoiceGroup
                             from inv in invoiceGroup.DefaultIfEmpty()
                             where inv?.CreatedDate.HasValue == true && inv.CreatedDate.Value.Month == m.Month
                             join id in db.InvoiceDetails on inv?.InvoiceID equals id?.InvoiceID into invoiceDetailsGroup
                             from id in invoiceDetailsGroup.DefaultIfEmpty()
                             join p in db.Products on id?.ProductID equals p?.ProductID into productsGroup
                             from prod in productsGroup.DefaultIfEmpty()
                             join c in db.Categories on prod?.CategoryID equals c?.CategoryID into categoriesGroup
                             from cat in categoriesGroup.DefaultIfEmpty()
                             join tc in db.TypeCategories on cat?.TypeCategoryID equals tc?.TypeCategoryID
                             where tc?.TypeCategoryName == "Quần nam"
                             group id by new { m.Month, Year = 2024 } into g
                             select new
                             {
                                 Month = g.Key.Month,
                                 Year = g.Key.Year,
                                 Revenue = g.Sum(x => x?.Quantity * x?.Price) ?? 0
                             };
            var revenueTC3 = from m in months
                             join i in db.Invoices on m.Month equals i.CreatedDate?.Month into invoiceGroup
                             from inv in invoiceGroup.DefaultIfEmpty()
                             where inv?.CreatedDate.HasValue == true && inv.CreatedDate.Value.Month == m.Month
                             join id in db.InvoiceDetails on inv?.InvoiceID equals id?.InvoiceID into invoiceDetailsGroup
                             from id in invoiceDetailsGroup.DefaultIfEmpty()
                             join p in db.Products on id?.ProductID equals p?.ProductID into productsGroup
                             from prod in productsGroup.DefaultIfEmpty()
                             join c in db.Categories on prod?.CategoryID equals c?.CategoryID into categoriesGroup
                             from cat in categoriesGroup.DefaultIfEmpty()
                             join tc in db.TypeCategories on cat?.TypeCategoryID equals tc?.TypeCategoryID
                             where tc?.TypeCategoryName == "Phụ kiện"
                             group id by new { m.Month, Year = 2024 } into g
                             select new
                             {
                                 Month = g.Key.Month,
                                 Year = g.Key.Year,
                                 Revenue = g.Sum(x => x?.Quantity * x?.Price) ?? 0
                             };
            var revenueTC4 = from m in months
                             join i in db.Invoices on m.Month equals i.CreatedDate?.Month into invoiceGroup
                             from inv in invoiceGroup.DefaultIfEmpty()
                             where inv?.CreatedDate.HasValue == true && inv.CreatedDate.Value.Month == m.Month
                             join id in db.InvoiceDetails on inv?.InvoiceID equals id?.InvoiceID into invoiceDetailsGroup
                             from id in invoiceDetailsGroup.DefaultIfEmpty()
                             join p in db.Products on id?.ProductID equals p?.ProductID into productsGroup
                             from prod in productsGroup.DefaultIfEmpty()
                             join c in db.Categories on prod?.CategoryID equals c?.CategoryID into categoriesGroup
                             from cat in categoriesGroup.DefaultIfEmpty()
                             join tc in db.TypeCategories on cat?.TypeCategoryID equals tc?.TypeCategoryID
                             where tc?.TypeCategoryName == "Giày dép"
                             group id by new { m.Month, Year = 2024 } into g
                             select new
                             {
                                 Month = g.Key.Month,
                                 Year = g.Key.Year,
                                 Revenue = g.Sum(x => x?.Quantity * x?.Price) ?? 0
                             };
            ViewData["revenueTC1"] = revenueTC1;
            ViewData["revenueTC2"] = revenueTC2;
            ViewData["revenueTC3"] = revenueTC3;
            ViewData["revenueTC4"] = revenueTC4;

            var topSellingProducts = (from p in db.Products
                                      join d in db.InvoiceDetails on p.ProductID equals d.ProductID
                                      group d by new { p.ProductID, p.ProductName } into g
                                      orderby g.Sum(x => x.Quantity) descending
                                      select new
                                      {
                                          g.Key.ProductID,
                                          g.Key.ProductName,
                                          TotalQuantitySold = g.Sum(x => x.Quantity)
                                      }).Take(8).ToList();

            ViewData["TopSellingProducts"] = topSellingProducts;

            return View();
        }
        private decimal GetRevenueByTypeCategory(string typeCategoryName)
        {
            return db.InvoiceDetails
                .Join(db.Products,
                      detail => detail.ProductID,
                      product => product.ProductID,
                      (detail, product) => new { detail, product })
                .Join(db.Categories,
                      combined => combined.product.CategoryID,
                      category => category.CategoryID,
                      (combined, category) => new { combined.detail, combined.product, category })
                .Join(db.TypeCategories,
                      combined => combined.category.TypeCategoryID,
                      typeCategory => typeCategory.TypeCategoryID,
                      (combined, typeCategory) => new { combined.detail, combined.product, typeCategory })
                .Join(db.Invoices,
                      combined => combined.detail.InvoiceID,
                      invoice => invoice.InvoiceID,
                      (combined, invoice) => new { combined.detail, combined.product, combined.typeCategory, invoice })
                .Where(invoice => invoice.invoice.CreatedDate.HasValue &&
                                  invoice.invoice.CreatedDate.Value.Year == DateTime.Now.Year &&
                                  invoice.typeCategory.TypeCategoryName == typeCategoryName)
                .Sum(x => (decimal?)x.detail.Quantity * x.detail.Price) ?? 0;
        }
    }
}