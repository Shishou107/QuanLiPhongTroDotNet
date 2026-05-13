using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Models;

public partial class VwInvoiceDetailFull
{
    public byte BillingMonth { get; set; }

    public short BillingYear { get; set; }

    public string BuildingName { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public string TenantName { get; set; } = null!;

    public string? ServiceName { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Amount { get; set; }
}
