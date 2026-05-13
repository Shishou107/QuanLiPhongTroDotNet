using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Models;

public partial class VwInvoiceOverview
{
    public Guid InvoiceId { get; set; }

    public string BuildingName { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public string TenantName { get; set; } = null!;

    public byte BillingMonth { get; set; }

    public short BillingYear { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal? RemainingAmount { get; set; }

    public string? InvoiceStatus { get; set; }

    public DateOnly DueDate { get; set; }
}
