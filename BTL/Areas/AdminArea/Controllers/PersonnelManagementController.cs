using BTL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Data.Entity;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BTL.Areas.AdminArea.Controllers
{
    public class PersonnelManagementController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/PersonnelManagement
        public ActionResult index(string search, int? page)
        {
            var ad = Session["Admin"] as Admin;
            if (ad == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            int pageSize = 5; // Số mục trên mỗi trang
            int pageNum = (page ?? 1); // Trang hiện tại, mặc định là 1
            ViewBag.CurrentSearch = search;
            ViewBag.error = "";

            // Lấy dữ liệu nhân viên
            var admins = from admin in db.Admins select admin;

            // Nếu có từ khóa tìm kiếm
            if (!String.IsNullOrEmpty(search))
            {
                admins = admins.Where(s => s.FullName.Contains(search));
                if (admins.Count() == 0)
                {
                    ViewBag.error = "Không tìm thấy tên nhân viên có ký tự " + "'" + search + "'";
                }
            }

            // Tạo dữ liệu phân trang
            var pagedAdmins = admins.ToList().OrderBy(n => n.AdminID).ToPagedList(pageNum, pageSize);

            // Tính toán danh sách các trang cần hiển thị (3 trang gần nhất)
            int totalPages = pagedAdmins.PageCount; // Tổng số trang
            int currentPage = pagedAdmins.PageNumber; // Trang hiện tại
            int startPage = Math.Max(1, currentPage - 1); // Bắt đầu từ trang trước trang hiện tại (nếu có)
            int endPage = Math.Min(totalPages, currentPage + 1); // Đến trang sau trang hiện tại (nếu có)

            ViewBag.PagesToShow = Enumerable.Range(startPage, endPage - startPage + 1).ToList();

            return View(pagedAdmins);
        }

        public ActionResult ExportToExcel(string search)
        {
            var ad = Session["Admin"] as Admin;
            if (ad == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var admins = from admin in db.Admins select admin;
            if (!string.IsNullOrEmpty(search))
            {
                admins = admins.Where(s => s.FullName.Contains(search));
            }

            var data = admins.OrderBy(n => n.AdminID).ToList();

            using (ExcelPackage excel = new ExcelPackage())
            {
                var ws = excel.Workbook.Worksheets.Add("Quản lý nhân viên");
                ws.Cells["A1"].Value = "ID";
                ws.Cells["B1"].Value = "Họ và tên";
                ws.Cells["C1"].Value = "Địa chỉ";
                ws.Cells["D1"].Value = "Số điện thoại";
                ws.Cells["E1"].Value = "Chức vụ";
                ws.Cells["F1"].Value = "Ảnh thẻ";

                ws.Cells["A1:F1"].Style.Font.Bold = true;
                ws.Cells["A1:F1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                int row = 2;
                string basePath = Server.MapPath("~/Content/images/admin/");

                foreach (var admin in data)
                {
                    ws.Cells[row, 1].Value = admin.AdminID;
                    ws.Cells[row, 2].Value = admin.FullName;
                    ws.Cells[row, 3].Value = admin.Address;
                    ws.Cells[row, 4].Value = admin.PhoneNumber;
                    ws.Cells[row, 5].Value = admin.Role;

                    string imagePath = Path.Combine(basePath, admin.URLPicture);
                    if (System.IO.File.Exists(imagePath))
                    {
                        var picture = ws.Drawings.AddPicture($"Image_{admin.AdminID}", new FileInfo(imagePath));
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

                string fileName = "DanhSachNhanVien.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream, contentType, fileName);
            }
        }


        public ActionResult insert()
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            return View();
        }
        [HttpPost]
        public ActionResult insert(Admin model, HttpPostedFileBase URLPicture)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (db.Admins.Any(a => a.Username == model.Username ||
                                   a.Email == model.Email ||
                                   a.PhoneNumber == model.PhoneNumber ||
                                   a.IdentityCard == model.IdentityCard))
            {
                ModelState.AddModelError("", "Dữ liệu đã tồn tại, vui lòng kiểm tra lại.");
                return View(model);
            }

            if (URLPicture != null && URLPicture.ContentLength > 0)
            {
                var fileName = Path.GetFileName(URLPicture.FileName);
                var path = Path.Combine(Server.MapPath("~/Content/images/admin/"), fileName);
                URLPicture.SaveAs(path);
                model.URLPicture = fileName;
            }

            try
            {
                db.Admins.Add(model);
                db.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        ModelState.AddModelError(validationError.PropertyName, validationError.ErrorMessage);
                    }
                }
                return View(model);
            }


            return RedirectToAction("index");
        }



        public ActionResult edit(int id)
        {
            var ad = Session["Admin"] as Admin;
            if (ad == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var admin = db.Admins.Find(id);
            return View(admin);
        }
        [HttpPost]
        public ActionResult edit(Admin admin, HttpPostedFileBase URLPicture)
        {
            var ad = Session["Admin"] as Admin;
            if (ad == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            try
            {
                var existingAdmin = db.Admins.Find(admin.AdminID);
                if (existingAdmin == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy nhân viên cần chỉnh sửa.");
                    return View(admin);
                }

                if (URLPicture != null && URLPicture.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(URLPicture.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/images/admin/"), fileName);

                    URLPicture.SaveAs(path);

                    admin.URLPicture = fileName;
                }
                else
                {
                    Console.WriteLine("Không có file mới được upload, giữ ảnh cũ.");
                }

                existingAdmin.FullName = admin.FullName;
                existingAdmin.Email = admin.Email;
                existingAdmin.Address = admin.Address;
                existingAdmin.PhoneNumber = admin.PhoneNumber;
                existingAdmin.BirthDate = admin.BirthDate;
                existingAdmin.HomeTown = admin.HomeTown;
                existingAdmin.IdentityCard = admin.IdentityCard;
                existingAdmin.Role = admin.Role;
                existingAdmin.Username = admin.Username;
                existingAdmin.Password = admin.Password;
                existingAdmin.URLPicture = admin.URLPicture;


                db.Entry(existingAdmin).State = EntityState.Modified;
                db.SaveChanges();
                Console.WriteLine("Cập nhật thành công.");


                Console.WriteLine("Cập nhật thành công.");
                return RedirectToAction("index");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi: " + ex.Message);
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin.");
                return View(admin);
            }
        }

        public ActionResult delete(int id)
        {
            var ad = Session["Admin"] as Admin;
            if (ad == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var admin = db.Admins.Find(id);
            if (admin == null)
            {
                return HttpNotFound();
            }

            db.Admins.Remove(admin);
            db.SaveChanges();

            return RedirectToAction("index");
        }

        public ActionResult detail(int id)
        {
            var ad = Session["Admin"] as Admin;
            if (ad == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var admin = db.Admins.Find(id);
            if (admin == null)
            {
                return HttpNotFound();
            }
            return View(admin);
        }
    }
}