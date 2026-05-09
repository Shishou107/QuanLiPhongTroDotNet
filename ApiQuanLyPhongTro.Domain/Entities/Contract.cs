using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Entities;

public partial class Contract
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Guid TenantId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal? DepositAmount { get; set; }

    public decimal AgreedPrice { get; set; }

    public int? Status { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Invoice>? Invoices { get; set; } = new List<Invoice>();

    public virtual Room? Room { get; set; } = null!;

    public virtual Tenant? Tenant { get; set; } = null!;
}
