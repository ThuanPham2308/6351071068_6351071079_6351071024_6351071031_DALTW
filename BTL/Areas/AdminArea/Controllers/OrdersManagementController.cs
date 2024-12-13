using BTL.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;
using OfficeOpenXml;
using System.IO;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.Web.Helpers;

namespace BTL.Areas.AdminArea.Controllers
{
    public class OrdersManagementController : Controller
    {
        MenShopEntities db = new MenShopEntities();
        // GET: AdminArea/OrdersManagement
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

            var invoices = from invoice in db.Invoices select invoice;

            if (!String.IsNullOrEmpty(search))
            {
                invoices = invoices.Where(s => s.Customer.FullName.Contains(search));
                if (invoices.Count() == 0)
                {
                    ViewBag.error = "Không tìm thấy tên khách hàng có ký tự " + "'" + search + "'";
                }
            }

            var pagedInvoices = invoices.ToList().OrderBy(n => n.InvoiceID).ToPagedList(pageNum, pageSize);

            int totalPages = pagedInvoices.PageCount;
            int currentPage = pagedInvoices.PageNumber;
            int startPage = Math.Max(1, currentPage - 1);
            int endPage = Math.Min(totalPages, currentPage + 1);

            ViewBag.PagesToShow = Enumerable.Range(startPage, endPage - startPage + 1).ToList();

            return View(pagedInvoices);
        }
        public ActionResult detail(int id)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceID == id);
            var invoiceDetails = db.InvoiceDetails
                   .Where(i => i.InvoiceID == id)
                   .ToList();

            if (invoice == null)
            {
                return HttpNotFound();
            }

            ViewData["invoiceDetails"] = invoiceDetails;
            return View(invoice);
        }

        public ActionResult ExportToExcel(string search)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var invoices = from invoice in db.Invoices select invoice;
            if (!string.IsNullOrEmpty(search))
            {
                invoices = invoices.Where(s => s.Customer.FullName.Contains(search));
            }

            var data = invoices.OrderBy(n => n.InvoiceID).ToList();

            using (ExcelPackage excel = new ExcelPackage())
            {
                var ws = excel.Workbook.Worksheets.Add("Quản lý đơn hàng");
                ws.Cells["A1"].Value = "ID";
                ws.Cells["B1"].Value = "Họ và tên khách hàng";
                ws.Cells["C1"].Value = "Địa chỉ";
                ws.Cells["D1"].Value = "Số điện thoại";
                ws.Cells["E1"].Value = "Tổng tiền";
                ws.Cells["E1"].Value = "Trạng thái";

                ws.Cells["A1:F1"].Style.Font.Bold = true;
                ws.Cells["A1:F1"].Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                int row = 2;

                foreach (var invoice in data)
                {
                    ws.Cells[row, 1].Value = invoice.InvoiceID;
                    ws.Cells[row, 2].Value = invoice.Customer.FullName;
                    ws.Cells[row, 3].Value = invoice.Customer.Address;
                    ws.Cells[row, 4].Value = invoice.Customer.PhoneNumber;
                    ws.Cells[row, 5].Value = invoice.TotalAmount + " VNĐ";
                    ws.Cells[row, 6].Value = invoice.Status;


                    ws.Row(row).Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    row++;
                }
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                excel.SaveAs(stream);
                stream.Position = 0;

                string fileName = "DanhSachDonHang.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(stream, contentType, fileName);
            }
        }

        public ActionResult Accept()
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            return View();
        }

        [HttpPost]
        public ActionResult Accept(int id, bool isAccept)
        {
            var admin = Session["Admin"] as Admin;
            if (admin == null)
            {
                return RedirectToAction("login", "Admin", new { area = "AdminArea" });
            }

            var invoice = db.Invoices.SingleOrDefault(i => i.InvoiceID == id);

            if (invoice == null)
            {
                return HttpNotFound("Invoice not found");
            }

            if (isAccept)
            {
                invoice.Status = "Đã xác nhận";

                var invoiceDetails = db.InvoiceDetails
                                       .Where(ide => ide.InvoiceID == invoice.InvoiceID)
                                       .ToList();

                foreach (var detail in invoiceDetails)
                {
                    var product = db.Products.SingleOrDefault(p => p.ProductID == detail.ProductID);

                    if (product != null)
                    {
                        product.QuantityInStock -= detail.Quantity;

                        if(product.QuantityInStock < 0)
                        {
                            product.QuantityInStock = 0;
                        }

                        var discountProduct = db.DiscountProducts
                            .FirstOrDefault(dp => dp.ProductID == detail.ProductID
                                && dp.DiscountProgram.StartDate <= DateTime.Now
                                && dp.DiscountProgram.EndDate >= DateTime.Now);

                        if (discountProduct != null)
                        {
                            discountProduct.QuantityDiscounted -= detail.Quantity;

                            if (discountProduct.QuantityDiscounted < 0)
                            {
                                discountProduct.QuantityDiscounted = 0; 
                            }
                        }

                        SendApologyEmail(invoice.Customer.Email, true, invoice.InvoiceID);
                    }
                }
            }
            else
            {
                invoice.Status = "Đã từ chối";
                SendApologyEmail(invoice.Customer.Email, false, invoice.InvoiceID);
            }

            db.SaveChanges();

            return RedirectToAction("index", "OrdersManagement", new {search = ""});
        }

        private void SendApologyEmail(string customerEmail, bool isAccept, int id)
        {
            var fromAddress = new MailAddress("quangvinhdang7a1@gmail.com", "Your Website");
            var toAddress = new MailAddress(customerEmail);
            const string subject = "Thông báo về trạng thái đơn hàng";
            string body;
            if(isAccept == false)
            {
                body = $"Kính gửi quý khách,\n\nRất tiếc, đơn hàng {id} của bạn đã bị từ chối -_- \nChúng tôi rất mong nhận được sự thông cảm của bạn.\n\nTrân trọng";
            }
            else
            {
                body = $"Kính gửi quý khách,\n\nĐơn hàng {id} của bạn đã được xác nhận!!!\nCảm ơn vì sự tin tưởng của bạn.\n\nTrân trọng";
            }

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
    }
}