//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BTL.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Customer
    {
        public Customer()
        {
            this.Carts = new HashSet<Cart>();
            this.Contacts = new HashSet<Contact>();
            this.Wishlists = new HashSet<Wishlist>();
            this.Invoices = new HashSet<Invoice>();
        }
    
        public int CustomerID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public System.DateTime BirthDate { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string VerificationCode { get; set; }
        public Nullable<System.DateTime> VerificationCodeExpiration { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    
        public virtual ICollection<Cart> Carts { get; set; }
        public virtual ICollection<Contact> Contacts { get; set; }
        public virtual ICollection<Wishlist> Wishlists { get; set; }
        public virtual ICollection<Invoice> Invoices { get; set; }
    }
}