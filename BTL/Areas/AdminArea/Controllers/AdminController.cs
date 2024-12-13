using BTL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Configuration;
using System.Web.Helpers;
using System.Runtime.Remoting.Contexts;


namespace BTL.Areas.AdminArea.Controllers
{
    public class AdminController : Controller
    {
        MenShopEntities db = new MenShopEntities();

        // GET: Admin
        public ActionResult index()
        {
            var admin = Session["Admin"] as Admin;
            if(admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            // Dashboard
            var numbersOfCustomer = db.Customers.Count();
            ViewBag.NumbersOfCustomer = numbersOfCustomer;
            var numbersOfProduct = db.Products.Count();
            ViewBag.NumbersOfProduct = numbersOfProduct;
            var numbersOfInvoice = db.Invoices.Count();
            ViewBag.NumbersOfInvoice = numbersOfInvoice;
            var almostOutOfStock = db.Products
                         .Where(p => p.QuantityInStock <= 10)
                         .Count();
            ViewBag.almostOutOfStock = almostOutOfStock;
            var newCustomers = db.Customers
                  .OrderByDescending(c => c.CreatedDate)
                  .Take(5)
                  .ToList();
            ViewData["newCustomers"] = newCustomers;

            //RevenueMonth
            var revenueMonth = db.Invoices
                .Where(invoice => invoice.CreatedDate.HasValue)
                .GroupBy(invoice => invoice.CreatedDate.Value.Month)
                .Select(group => new
                {
                    Month = group.Key,
                    Revenue = group.Sum(invoice => invoice.TotalAmount)
                })
                .ToDictionary(x => x.Month, x => x.Revenue);

            ViewBag.RevenueJanuary = revenueMonth.ContainsKey(1) ? revenueMonth[1] : 0;
            ViewBag.RevenueFebruary = revenueMonth.ContainsKey(2) ? revenueMonth[2] : 0;
            ViewBag.RevenueMarch = revenueMonth.ContainsKey(3) ? revenueMonth[3] : 0;
            ViewBag.RevenueApril = revenueMonth.ContainsKey(4) ? revenueMonth[4] : 0;
            ViewBag.RevenueMay = revenueMonth.ContainsKey(5) ? revenueMonth[5] : 0;
            ViewBag.RevenueJune = revenueMonth.ContainsKey(6) ? revenueMonth[6] : 0;
            ViewBag.RevenueJuly = revenueMonth.ContainsKey(7) ? revenueMonth[7] : 0;
            ViewBag.RevenueAugust = revenueMonth.ContainsKey(8) ? revenueMonth[8] : 0;
            ViewBag.RevenueSeptember = revenueMonth.ContainsKey(9) ? revenueMonth[9] : 0;
            ViewBag.RevenueOctober = revenueMonth.ContainsKey(10) ? revenueMonth[10] : 0;
            ViewBag.RevenueNovember = revenueMonth.ContainsKey(11) ? revenueMonth[11] : 0;
            ViewBag.RevenueDecember = revenueMonth.ContainsKey(12) ? revenueMonth[12] : 0;

            // NewCustomerMonth
            var newCustomersMonth = db.Customers
                .Where(customer => customer.CreatedDate.HasValue)
                .GroupBy(customer => customer.CreatedDate.Value.Month)
                .Select(group => new
                {
                    Month = group.Key,
                    CustomerCount = group.Count()
                })
                .ToDictionary(x => x.Month, x => x.CustomerCount);

            ViewBag.newCustomersJanuary = newCustomersMonth.ContainsKey(1) ? newCustomersMonth[1] : 0;
            ViewBag.newCustomersFebruary = newCustomersMonth.ContainsKey(2) ? newCustomersMonth[2] : 0;
            ViewBag.newCustomersMarch = newCustomersMonth.ContainsKey(3) ? newCustomersMonth[3] : 0;
            ViewBag.newCustomersApril = newCustomersMonth.ContainsKey(4) ? newCustomersMonth[4] : 0;
            ViewBag.newCustomersMay = newCustomersMonth.ContainsKey(5) ? newCustomersMonth[5] : 0;
            ViewBag.newCustomersJune = newCustomersMonth.ContainsKey(6) ? newCustomersMonth[6] : 0;
            ViewBag.newCustomersJuly = newCustomersMonth.ContainsKey(7) ? newCustomersMonth[7] : 0;
            ViewBag.newCustomersAugust = newCustomersMonth.ContainsKey(8) ? newCustomersMonth[8] : 0;
            ViewBag.newCustomersSeptember = newCustomersMonth.ContainsKey(9) ? newCustomersMonth[9] : 0;
            ViewBag.newCustomersOctober = newCustomersMonth.ContainsKey(10) ? newCustomersMonth[10] : 0;
            ViewBag.newCustomersNovember = newCustomersMonth.ContainsKey(11) ? newCustomersMonth[11] : 0;
            ViewBag.newCustomersDecember = newCustomersMonth.ContainsKey(12) ? newCustomersMonth[12] : 0;



            //Management
            var products = db.Products
                             .OrderByDescending(p => p.CreatedDate) 
                             .Take(5) 
                             .ToList();
            ViewData["products"] = products;
            var admins = db.Admins.ToList();
            ViewData["admins"] = admins;
            var customers = db.Customers.ToList();
            ViewData["customers"] = customers;
            return View();
        }

        public ActionResult login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult login(string username, string password)
        {
            var admin = db.Admins.SingleOrDefault(a => a.Username == username);

            if (admin != null && admin.Password == password) 
            {
                Session["Admin"] = admin;
                return RedirectToAction("index", "Admin", new { area = "AdminArea" });
            }
            else
            {
                ViewBag.ErrorMessage = "Tài khoản hoặc mật khẩu không đúng.";
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session["Admin"] = null;
            return RedirectToAction("login", "Admin", new { area = "AdminArea" });
        }


        public ActionResult forgetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult forgetPassword(string email)
        {
            var user = db.Admins.FirstOrDefault(a => a.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại trong hệ thống.";
                return View();
            }

            var verificationCode = GenerateVerificationCode();
            user.VerificationCode = verificationCode;
            user.VerificationCodeExpiration = DateTime.Now.AddMinutes(10);
            db.SaveChanges();

            Session["Email"] = email;
            Session["VerificationCode"] = verificationCode;

            SendVerificationEmail(email, verificationCode);

            ViewBag.Message = "Mã xác thực đã được gửi đến email của bạn.";

            TempData.Keep("Email");
            TempData.Keep("VerificationCode");

            return RedirectToAction("verifyCode");
        }

       private string GenerateVerificationCode()
        {
            var random = new Random();
            var code = random.Next(100000, 999999).ToString(); 
            return code;
        }

        private void SendVerificationEmail(string email, string verificationCode)
        {
            var fromAddress = new MailAddress("quangvinhdang7a1@gmail.com", "Your Website");
            var toAddress = new MailAddress(email);
            const string subject = "Mã xác thực quên mật khẩu";
            string body = $"Mã xác thực của bạn là: {verificationCode}";

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

        public ActionResult verifyCode()
        {
            var email = Session["Email"] as string;
            var verificationCode = Session["VerificationCode"] as string;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(verificationCode))
            {
                ViewBag.ErrorMessage = "Email hoặc mã xác thực không hợp lệ.";
                return View();
            }

            ViewBag.Email = email;
            ViewBag.VerificationCode = verificationCode;

            return View();
        }



        [HttpPost]
        public ActionResult verifyCode(string email, string verificationCode)
        {
            var user = db.Admins.FirstOrDefault(a => a.Email == email && a.VerificationCode == verificationCode);

            if (user == null || user.VerificationCodeExpiration < DateTime.Now)
            {
                ViewBag.ErrorMessage = "Mã xác thực sai hoặc hết hạn.";
                return View();
            }

            return RedirectToAction("resetPassword");
        }

        public ActionResult resetPassword()
        {
            var email = Session["Email"] as string;
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Email không hợp lệ.";
                return View();
            }

            ViewBag.Email = email;

            return View();
        }



        [HttpPost]
        public ActionResult resetPassword(string email, string newPassword, string reNewPassword)
        {
            var sessionEmail = Session["Email"] as string;
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                ViewBag.ErrorMessage = "Email không hợp lệ.";
                return resetPassword();
            }

            if (newPassword != reNewPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                return resetPassword();
            }

            if (newPassword.Length < 8)
            {
                ViewBag.ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.";
                return resetPassword();
            }

            var user = db.Admins.FirstOrDefault(a => a.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại.";
                return resetPassword();
            }

            user.Password = newPassword;
            user.VerificationCode = null; 
            user.VerificationCodeExpiration = null; 
            db.SaveChanges();

            Session.Remove("Email");
            Session.Remove("VerificationCode");

            ViewBag.Message = "Mật khẩu đã được thay đổi thành công.";
            return RedirectToAction("login");
        }


    }
}