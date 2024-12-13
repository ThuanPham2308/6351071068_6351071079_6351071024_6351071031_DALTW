using BTL.Models;
using OfficeOpenXml;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTL.Areas.AdminArea.Controllers
{
    public class CommentsController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/Comments
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
            var contacts = from contact in db.Contacts select contact;
            ViewBag.error = "";
            if (!String.IsNullOrEmpty(search))
            {
                contacts = contacts.Where(s => s.Customer.FullName.Contains(search));
                if (contacts.Count() == 0)
                {
                    ViewBag.error = "Không tìm thấy tên khách hàng có ký tự " + "'" + search + "'";
                }
            }

            return View(contacts.ToList().OrderBy(n => n.AdminID).ToPagedList(pageNum, pageSize));
        }
        public ActionResult ExportToExcel(string search)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var comments = from comment in db.Contacts select comment;
            if (!string.IsNullOrEmpty(search))
            {
                comments = comments.Where(s => s.Customer.FullName.Contains(search));
            }

            var data = comments.OrderBy(n => n.AdminID).ToList();

            using (ExcelPackage excel = new ExcelPackage())
            {
                var ws = excel.Workbook.Worksheets.Add("Quản lý ý kiến đóng góp");
                ws.Cells["A1"].Value = "ID";
                ws.Cells["B1"].Value = "Họ và tên khách hàng";
                ws.Cells["C1"].Value = "Chủ đề";
                ws.Cells["D1"].Value = "Ý kiến đóng góp";
          

                ws.Cells["A1:D1"].Style.Font.Bold = true;
                ws.Cells["A1:D1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                int row = 2;

                foreach (var comment in data)
                {
                    ws.Cells[row, 1].Value = comment.ContactID;
                    ws.Cells[row, 2].Value = comment.Customer.FullName;
                    ws.Cells[row, 3].Value = comment.Topic;
                    ws.Cells[row, 4].Value = comment.Message;

                    ws.Row(row).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    row++;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                excel.SaveAs(stream);
                stream.Position = 0;

                string fileName = "DanhSachYKienDongGop.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream, contentType, fileName);
            }
        }
    }
}