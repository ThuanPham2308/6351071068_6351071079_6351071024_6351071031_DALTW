using BTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;
using System.IO;
using OfficeOpenXml;
using System.IO;

namespace BTL.Areas.AdminArea.Controllers
{
    public class CustomersManagementController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/CustomersManagement
        public ActionResult index(string search, int? page)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            int pageSize = 5;
            int pageNum = (page ?? 1);
            ViewBag.CurrentSearch = search;
            ViewBag.error = "";

            var customers = from customer in db.Customers select customer;

            if (!String.IsNullOrEmpty(search))
            {
                customers = customers.Where(s => s.FullName.Contains(search));
                if (customers.Count() == 0)
                {
                    ViewBag.error = "Không tìm thấy tên khách hàng có ký tự " + "'" + search + "'";
                }
            }

            var pagedCustomers = customers.ToList().OrderBy(n => n.CustomerID).ToPagedList(pageNum, pageSize);

            int totalPages = pagedCustomers.PageCount;
            int currentPage = pagedCustomers.PageNumber;
            int startPage = Math.Max(1, currentPage - 1);
            int endPage = Math.Min(totalPages, currentPage + 1);

            ViewBag.PagesToShow = Enumerable.Range(startPage, endPage - startPage + 1).ToList();

            return View(pagedCustomers);
        }
        public ActionResult detail(int id)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var customer = db.Customers.Find(id);
            if (customer == null)
            {
                return HttpNotFound();
            }
            return View(customer);
        }
        public ActionResult ExportToExcel(string search)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var customers = from customer in db.Customers select customer;
            if (!string.IsNullOrEmpty(search))
            {
                customers = customers.Where(s => s.FullName.Contains(search));
            }

            var data = customers.OrderBy(n => n.CustomerID).ToList();

            using (ExcelPackage excel = new ExcelPackage())
            {
                var ws = excel.Workbook.Worksheets.Add("Quản lý khách hàng");
                ws.Cells["A1"].Value = "ID";
                ws.Cells["B1"].Value = "Họ và tên";
                ws.Cells["C1"].Value = "Địa chỉ";
                ws.Cells["D1"].Value = "Số điện thoại";
                ws.Cells["E1"].Value = "Ngày sinh";

                ws.Cells["A1:E1"].Style.Font.Bold = true;
                ws.Cells["A1:E1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                int row = 2;

                foreach (var customer in data)
                {
                    ws.Cells[row, 1].Value = customer.CustomerID;
                    ws.Cells[row, 2].Value = customer.FullName;
                    ws.Cells[row, 3].Value = customer.Address;
                    ws.Cells[row, 4].Value = customer.PhoneNumber;
                    ws.Cells[row, 5].Value = customer.BirthDate.ToString("dd-MM-yyyy");


                    ws.Row(row).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    row++;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                excel.SaveAs(stream);
                stream.Position = 0;

                string fileName = "DanhSachKhachHang.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream, contentType, fileName);
            }
        }
    }
}