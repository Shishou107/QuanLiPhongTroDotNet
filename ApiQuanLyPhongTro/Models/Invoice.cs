using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Models;

public partial class Invoice
{
    public Guid Id { get; set; }

    public Guid ContractId { get; set; }

    public byte BillingMonth { get; set; }

    public short BillingYear { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public byte Status { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Contract Contract { get; set; } = null!;

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
