using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Models;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public byte PaymentMethod { get; set; }

    public string? ReferenceCode { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
