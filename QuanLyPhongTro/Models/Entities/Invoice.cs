using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.Entities;

public class Invoice
{
    public Guid Id { get; set; }

    [Required]
    public Guid ContractId { get; set; }

    [Required]
    public int BillingMonth { get; set; }

    [Required]
    public int BillingYear { get; set; }

    [Required]
    public decimal TotalAmount { get; set; }

    public decimal? PaidAmount { get; set; }

    public int? Status { get; set; } // 0=Unpaid, 1=Partial, 2=Paid, 3=Overdue

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public virtual Contract? Contract { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
