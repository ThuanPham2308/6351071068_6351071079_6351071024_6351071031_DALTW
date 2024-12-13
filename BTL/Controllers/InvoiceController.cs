using BTL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using PagedList;
using PagedList.Mvc;

namespace BTL.Controllers
{
    public class InvoiceController : Controller
    {
        MenShopEntities menShopEntities = new MenShopEntities();
        // GET: Invoice
        public ActionResult Index(int? page)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int pageSize = 5;

            int pageNumber = (page ?? 1);

            // Lấy danh sách hóa đơn của khách hàng
            var invoiceList = menShopEntities.Invoices
                                             .Where(c => c.CustomerID == customer.CustomerID)
                                             .OrderByDescending(i => i.CreatedDate) 
                                             .ToPagedList(pageNumber, pageSize);

            return View(invoiceList);
        }

        public ActionResult Detail(int id)
        {
            var customer = Session["Customer"] as Customer;
            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var invoice = menShopEntities.Invoices
                                         .Include(i => i.InvoiceDetails.Select(d => d.Product)) 
                                         .FirstOrDefault(i => i.InvoiceID == id && i.CustomerID == customer.CustomerID);

            if (invoice == null)
            {
                return HttpNotFound();
            }

            return View(invoice);
        }

    }
}