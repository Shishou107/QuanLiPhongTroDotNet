using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Entities;

public partial class InvoiceDetail
{
    public Guid Id { get; set; }

    public Guid InvoiceId { get; set; }

    public Guid? ServiceId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; } = null!;

    public virtual Service? Service { get; set; }
}
