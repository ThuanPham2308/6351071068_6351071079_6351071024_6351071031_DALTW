using BTL.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Runtime.CompilerServices;

namespace BTL.Controllers
{
    public class AccountController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        // GET: Account
        public ActionResult Index()
        {
            var customer = Session["Customer"] as Customer; 
            if(customer == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View(customer);
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            var customer = menShopEntities.Customers.SingleOrDefault(a => a.Username == username);

            if (customer != null && customer.Password == password)
            {
                Session["Customer"] = customer;

                return RedirectToAction("index", "Home");
            }
            else
            {
                ViewBag.ErrorMessage = "Tài khoản hoặc mật khẩu không đúng.";
                return View();
            }
        }

        public ActionResult Logout()
        {
            Session["Customer"] = null;

            return RedirectToAction("index", "Home");
        }


        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(string username, string password, string re_password, string email, string phoneNumber, string fullName, DateTime birthday, string address)
        {
            // Kiểm tra trùng lặp tên người dùng hoặc email
            var existingUserName = menShopEntities.Customers
                                .SingleOrDefault(c => c.Username == username);
            if (existingUserName != null)
            {
                ViewBag.ErrorMessageUserName = "Tên người dùng đã được sử dụng.";
                return View();
            }

            // Kiểm tra mật khẩu khớp
            if (password != re_password)
            {
                ViewBag.ErrorMessagePassword = "Mật khẩu và xác nhận mật khẩu không khớp.";
                return View();
            }

            // Kiểm tra số điện thoại hợp lệ
            if (!phoneNumber.All(char.IsDigit) || phoneNumber.Length < 10 || phoneNumber.Length > 11)
            {
                ViewBag.ErrorMessagePhoneNumber = "Số điện thoại không hợp lệ.";
                return View();
            }

            // Kiểm tra trùng email
            var existingPhone = menShopEntities.Customers
                                .SingleOrDefault(c => c.PhoneNumber == phoneNumber);
            if (existingPhone != null)
            {
                ViewBag.ErrorMessagePhoneNumber = "Số điện thoại đã tồn tại.";
                return View();
            }

            // Kiểm tra ngày sinh hợp lệ
            if (birthday > DateTime.Now)
            {
                ViewBag.ErrorMessageBirthday = "Ngày sinh không hợp lệ.";
                return View();
            }

            // Kiểm tra trùng email
            var existingEmail = menShopEntities.Customers
                                .SingleOrDefault(c => c.Email == email);
            if (existingEmail != null)
            {
                ViewBag.ErrorMessageEmail = "Email đã được sử dụng.";
                return View();
            }

            // Tạo khách hàng mới
            var newCustomer = new Customer
            {
                Username = username,
                Password = password, // TODO: Mã hóa mật khẩu trước khi lưu
                Email = email,
                PhoneNumber = phoneNumber,
                FullName = fullName,
                BirthDate = birthday,
                Address = address,
                CreatedDate = DateTime.Now // Ngày tạo
            };

            // Thêm vào cơ sở dữ liệu
            menShopEntities.Customers.Add(newCustomer);
            menShopEntities.SaveChanges();

            //TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login", "Account");
        }



        public ActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ForgetPassword(string email)
        {
            var user = menShopEntities.Customers.SingleOrDefault(a => a.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại trong hệ thống.";
                return View();
            }

            // Tạo mã xác thực mới và thiết lập thời gian hết hạn
            var verificationCode = GenerateVerificationCode();
            user.VerificationCode = verificationCode;
            user.VerificationCodeExpiration = DateTime.Now.AddMinutes(10); // Hết hạn sau 10 phút
            menShopEntities.SaveChanges();

            // Set TempData for email and verificationCode
            //TempData["Email"] = email;
            //TempData["VerificationCode"] = verificationCode;

            Session["Email"] = email;
            Session["VerificationCode"] = verificationCode;

            // Gửi mã qua email
            SendVerificationEmail(email, verificationCode);

            ViewBag.Message = "Mã xác thực đã được gửi đến email của bạn.";

            // Ensure TempData is kept for next request
            TempData.Keep("Email");
            TempData.Keep("VerificationCode");

            return RedirectToAction("VerifyCode", "Account");
        }



        // Hàm tạo mã xác thực ngẫu nhiên
        private string GenerateVerificationCode()
        {
            var random = new Random();
            var code = random.Next(100000, 999999).ToString(); // Mã 6 chữ số
            return code;
        }

        // Hàm gửi email
        private void SendVerificationEmail(string email, string verificationCode)
        {
            var fromAddress = new MailAddress("quangvinhdang7a1@gmail.com", "Your Website");
            var toAddress = new MailAddress(email);
            const string subject = "Mã xác thực quên mật khẩu";
            string body = $"Mã xác thực của bạn là: {verificationCode}";

            var username = ConfigurationManager.AppSettings["EmailUsername"];
            var appPassword = ConfigurationManager.AppSettings["EmailPassword"]; // Sử dụng mật khẩu ứng dụng
            var smtpHost = "smtp.gmail.com";  // Sử dụng Gmail SMTP host
            var smtpPort = 587;  // Cổng 587 cho TLS

            using (var smtp = new SmtpClient(smtpHost, smtpPort))
            {
                smtp.Credentials = new NetworkCredential(username, appPassword); // Sử dụng mật khẩu ứng dụng
                smtp.EnableSsl = true;
                smtp.Send(fromAddress.Address, toAddress.Address, subject, body);
            }
        }



        public ActionResult VerifyCode()
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
        public ActionResult VerifyCode(string email, string verificationCode)
        {
            var user = menShopEntities.Customers.SingleOrDefault(a => a.Email == email && a.VerificationCode == verificationCode);

            if (user == null || user.VerificationCodeExpiration < DateTime.Now)
            {
                ViewBag.ErrorMessage = "Mã xác thực sai hoặc hết hạn.";
                return View();
            }

            return RedirectToAction("ResetPassword", "Account");
        }




        public ActionResult ResetPassword()
        {
            var email = Session["Email"] as string;
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Email không hợp lệ.";
                return View();
            }

            // Pass email to the view
            ViewBag.Email = email;

            return View();
        }


        [HttpPost]
        public ActionResult ResetPassword(string email, string newPassword, string reNewPassword)
        {
            // Kiểm tra xem email có tồn tại trong session không
            var sessionEmail = Session["Email"] as string;
            if (string.IsNullOrEmpty(sessionEmail) || sessionEmail != email)
            {
                ViewBag.ErrorMessage = "Email không hợp lệ.";
                //return View();
                return ResetPassword();
            }

            // Kiểm tra mật khẩu mới và mật khẩu xác nhận có khớp hay không
            if (newPassword != reNewPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                return ResetPassword();
                //return View();
            }

            // Kiểm tra độ dài mật khẩu mới (ví dụ: ít nhất 8 ký tự)
            if (newPassword.Length < 8)
            {
                ViewBag.ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự.";
                return ResetPassword();
                //return View();
            }

            // Kiểm tra xem email có tồn tại trong hệ thống không
            var user = menShopEntities.Customers.SingleOrDefault(a => a.Email == email);
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email không tồn tại.";
                return ResetPassword();
                //return View();
            }

            // Cập nhật mật khẩu mới
            user.Password = newPassword; // Bạn có thể mã hóa mật khẩu ở đây
            user.VerificationCode = null; // Xóa mã xác thực
            user.VerificationCodeExpiration = null; // Xóa thời gian hết hạn của mã
            menShopEntities.SaveChanges();

            // Xóa thông tin xác thực khỏi session
            Session.Remove("Email");
            Session.Remove("VerificationCode");

            ViewBag.Message = "Mật khẩu đã được thay đổi thành công.";
            return RedirectToAction("Login", "Account");
        }

        public ActionResult Detail()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return PartialView(customer);
        }

        public ActionResult Edit()
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Customer c = menShopEntities.Customers.SingleOrDefault(x => x.CustomerID == customer.CustomerID);

            return PartialView(c);
        }


        [HttpPost]
        public ActionResult Edit(string fullName, DateTime birthday, string phoneNumber, string email, string address)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            Customer c = menShopEntities.Customers.SingleOrDefault(x => x.CustomerID == customer.CustomerID);

            if (!phoneNumber.All(char.IsDigit) || phoneNumber.Length < 10 || phoneNumber.Length > 11)
            {
                ViewBag.ErrorMessagePhoneNumber = "Số điện thoại không hợp lệ.";
                return View(c);
            }
            if (menShopEntities.Customers.Any(x => x.PhoneNumber == phoneNumber && x.CustomerID != c.CustomerID))
            {
                ViewBag.ErrorMessagePhoneNumber = "Số điện thoại đã được sử dụng.";
                return View(c);
            }
            if (menShopEntities.Customers.Any(x => x.Email == email && x.CustomerID != c.CustomerID))
            {
                ViewBag.ErrorMessageEmail = "Email đã được sử dụng.";
                return View(c);
            }

            c.FullName = fullName;   
            c.Address = address;
            c.Email = email;
            c.PhoneNumber = phoneNumber; 
            c.BirthDate = birthday;

            menShopEntities.SaveChanges();

            return RedirectToAction("Index", "Account");
        }

    }
}