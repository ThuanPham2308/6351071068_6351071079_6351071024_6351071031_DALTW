using BTL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BTL.Areas.AdminArea.Controllers
{
    public class SettingsController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/Settings
        public ActionResult index(int id)
        {
            var admin = db.Admins.Find(id);
            return View(admin);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateSettings(Admin admin, HttpPostedFileBase URLPicture)
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

            return RedirectToAction("index", new { id = admin.AdminID });
        }

        public ActionResult Dictionary()
        {
            var admin = Session["Admin"] as Admin;
            if(admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }



            return View();
        }
    }
}