using BTL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;
using OfficeOpenXml;
using System.Security.Cryptography.X509Certificates;
using System.Data.Entity;

namespace BTL.Areas.AdminArea.Controllers
{
    public class ProductsManagementController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/ProductsManagement
        public ActionResult index(string search, int? page)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            int pageSize = 10;
            int pageNum = (page ?? 1);
            ViewBag.CurrentSearch = search;
            var products = from product in db.Products select product;
            ViewBag.error = "";
            if (!String.IsNullOrEmpty(search))
            {
                products = products.Where(s => s.ProductName.Contains(search));
                if (products.Count() == 0)
                {
                    ViewBag.error = "Không tìm thấy tên sản phẩm có ký tự " + "'" + search + "'";
                }
            }

            var pagedProducts = products.ToList().OrderBy(n => n.ProductID).ToPagedList(pageNum, pageSize);

            int totalPages = pagedProducts.PageCount;
            int currentPage = pagedProducts.PageNumber;
            int startPage = Math.Max(1, currentPage - 1);
            int endPage = Math.Min(totalPages, currentPage + 1);

            ViewBag.PagesToShow = Enumerable.Range(startPage, endPage - startPage + 1).ToList();

            return View(pagedProducts);
        }
        public ActionResult detail(int id)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }
        public ActionResult insert()
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var categories = db.Categories.ToList();
            return View(categories);
        }

        [HttpPost]
        public ActionResult insert(string ProductName, int CategoryID, decimal Price, int QuantityInStock, string Description, HttpPostedFileBase URLPicture)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            if (string.IsNullOrEmpty(ProductName) || Price <= 0 || QuantityInStock < 0 || CategoryID <= 0)
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin hợp lệ.");
                return View();
            }

            var newProduct = new Product
            {
                ProductName = ProductName,
                CategoryID = CategoryID,
                Price = Price,
                QuantityInStock = QuantityInStock,
                Description = Description,
                URLPicture = "",
                CreatedDate = DateTime.Now
            };

            if (URLPicture != null && URLPicture.ContentLength > 0)
            {
                var fileName = Path.GetFileName(URLPicture.FileName);
                var typeCategoryFolder = db.TypeCategories
                            .Where(c => c.Categories.Any(d => d.CategoryID == CategoryID))
                            .Select(c => c.TypeCategoryName) 
                            .FirstOrDefault();


                var categoryFolder = db.Categories
                       .Where(c => c.CategoryID == CategoryID)
                       .Select(c => c.CategoryName) 
                       .FirstOrDefault();

                var path = Path.Combine(Server.MapPath($"~/Content/images/{typeCategoryFolder}/{categoryFolder}/"), fileName);
                URLPicture.SaveAs(path);
                newProduct.URLPicture = fileName; 
            }

            try
            {
                db.Products.Add(newProduct);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã có lỗi xảy ra khi thêm sản phẩm: " + ex.Message);
                return View();
            }

            return RedirectToAction("index");
        }



        public ActionResult Edit(int id)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var categories = db.Categories.ToList();

            ViewBag.Product = product;
            ViewBag.CategoryList = categories;
            return View(product);
        }

        [HttpPost]
        public ActionResult Edit(int id, string ProductName, int CategoryID, decimal Price, int QuantityInStock, string Description, HttpPostedFileBase URLPicture)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var product = db.Products.SingleOrDefault(p => p.ProductID == id);

            product.ProductName = ProductName;
            product.CategoryID = CategoryID;
            product.Price = Price;
            product.QuantityInStock = QuantityInStock;
            product.Description = Description;

            // Xử lý file ảnh sản phẩm
            if (URLPicture != null && URLPicture.ContentLength > 0)
            {
                var fileName = Path.GetFileName(URLPicture.FileName);
                var typeCategoryFolder = db.TypeCategories
                            .Where(c => c.Categories.Any(d => d.CategoryID == CategoryID))
                            .Select(c => c.TypeCategoryName)
                            .FirstOrDefault();


                var categoryFolder = db.Categories
                       .Where(c => c.CategoryID == CategoryID)
                       .Select(c => c.CategoryName)
                       .FirstOrDefault();

                var path = Path.Combine(Server.MapPath($"~/Content/images/{typeCategoryFolder}/{categoryFolder}/"), fileName);
                URLPicture.SaveAs(path);
                product.URLPicture = fileName;
            }

            db.SaveChanges();
            return RedirectToAction("index");
        }


        public ActionResult delete(int id)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            db.Products.Remove(product);
            db.SaveChanges();

            return RedirectToAction("index");
        }
        public ActionResult add()
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var products = db.Products.ToList();
            ViewBag.products = products;
            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult add(Product product)
        //{
        //    var admin = Session["Admin"] as Admin;
        //    if (admin == null)
        //    {
        //        return RedirectToAction("login", "Admin", new { area = "AdminArea" });
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        var existingProduct = db.Products.SingleOrDefault(p => p.ProductID == product.ProductID);

        //        if (existingProduct != null)
        //        {
        //            existingProduct.QuantityInStock += product.QuantityInStock;

        //            db.SaveChanges();
        //        }

        //        return RedirectToAction("index", "ProductsManagement");
        //    }
        //    var products = db.Products.ToList();
        //    ViewBag.products = products;

        //    return View(product);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult add(int id, int quantity)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var products = db.Products.ToList();
            ViewBag.products = products;

            if (ModelState.IsValid)
            {
                if (quantity <= 0)
                {
                    return HttpNotFound();
                }

                var existingProduct = db.Products.SingleOrDefault(p => p.ProductID == id);

                if (existingProduct != null)
                {
                    existingProduct.QuantityInStock += quantity;

                    db.SaveChanges();
                }

                return RedirectToAction("index", "ProductsManagement");
            }

            return HttpNotFound();
        }

        public ActionResult ExportToExcel(string search)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var products = from product in db.Products select product;
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(s => s.ProductName.Contains(search));
            }

            var data = products.OrderBy(n => n.ProductID).ToList();

            using (ExcelPackage excel = new ExcelPackage())
            {
                var ws = excel.Workbook.Worksheets.Add("Quản lý sản phẩm");
                ws.Cells["A1"].Value = "ID";
                ws.Cells["B1"].Value = "Tên sản phẩm";
                ws.Cells["C1"].Value = "Tồn kho";
                ws.Cells["D1"].Value = "Giá bán";
                ws.Cells["E1"].Value = "Danh mục";
                ws.Cells["F1"].Value = "Ảnh";

                ws.Cells["A1:F1"].Style.Font.Bold = true;
                ws.Cells["A1:F1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                int row = 2;
                string basePath = Server.MapPath("~/Content/images/admin/");

                foreach (var product in data)
                {
                    ws.Cells[row, 1].Value = product.ProductID;
                    ws.Cells[row, 2].Value = product.ProductName;
                    ws.Cells[row, 3].Value = product.QuantityInStock;
                    ws.Cells[row, 4].Value = product.Price;
                    ws.Cells[row, 5].Value = product.Category.CategoryName;

                    string imagePath = Path.Combine(basePath, product.URLPicture);
                    if (System.IO.File.Exists(imagePath))
                    {
                        var picture = ws.Drawings.AddPicture($"Image_{product.ProductID}", new FileInfo(imagePath));
                        picture.SetPosition(row - 1, 0, 6 - 1, 0);
                        picture.SetSize(90, 120);
                        ws.Row(row).Height = 100;
                    }
                    ws.Row(row).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    row++;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                excel.SaveAs(stream);
                stream.Position = 0;

                string fileName = "DanhSachSanPham.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream, contentType, fileName);
            }
        }
        public ActionResult sale()
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var products = db.Products.Select(p => new { p.ProductID, p.ProductName }).ToList();
            ViewBag.Products = products;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Sale(DiscountProgram model, List<int> SelectedProducts, List<int> Quantities)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            if (ModelState.IsValid)
            {
                db.DiscountPrograms.Add(model);
                db.SaveChanges();

                if (SelectedProducts != null)
                {
                    for (int i = 0; i < SelectedProducts.Count; i++)
                    {
                        int productId = SelectedProducts[i];
                        int quantity = Quantities[i];

                        db.DiscountProducts.Add(new DiscountProduct
                        {
                            DiscountProgramID = model.DiscountProgramID,
                            ProductID = productId,
                            QuantityDiscounted = quantity
                        });
                    }
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            ViewBag.Products = db.Products.Select(p => new { p.ProductID, p.ProductName }).ToList();
            return View(model);
        }

    }
}
