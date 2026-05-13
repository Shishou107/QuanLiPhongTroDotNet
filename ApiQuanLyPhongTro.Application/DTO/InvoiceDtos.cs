using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Application.DTO;

public class InvoiceListDto
{
    public Guid Id { get; set; }
    public string? InvoiceCode { get; set; }
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string BuildingName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount => TotalAmount - PaidAmount;
    public DateOnly? DueDate { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Chưa thanh toán",
        1 => "Thanh toán một phần",
        2 => "Đã thanh toán",
        3 => "Quá hạn",
        _ => "Không xác định"
    };
}

public class InvoiceDetailDto
{
    public Guid Id { get; set; }
    public string? InvoiceCode { get; set; }
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public ContractListDto? Contract { get; set; }
    public TenantListDto? Tenant { get; set; }
    public RoomListDto? Room { get; set; }
    public List<InvoiceDetailItemDto> Details { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount => TotalAmount - PaidAmount;
    public DateOnly? DueDate { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Chưa thanh toán",
        1 => "Thanh toán một phần",
        2 => "Đã thanh toán",
        3 => "Quá hạn",
        _ => "Không xác định"
    };
}

public class InvoiceDetailItemDto
{
    public Guid Id { get; set; }
    public Guid? ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CreateInvoiceDto
{
    public Guid ContractId { get; set; }
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public DateOnly? DueDate { get; set; }
    public List<CreateInvoiceDetailDto> Details { get; set; } = new();
}

public class CreateInvoiceDetailDto
{
    public Guid ServiceId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}

public class UpdateInvoiceDto
{
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public DateOnly? DueDate { get; set; }
    public int Status { get; set; }
    public List<UpdateInvoiceDetailDto> Details { get; set; } = new();
}

public class UpdateInvoiceDetailDto
{
    public Guid? Id { get; set; } // Null for new items
    public Guid ServiceId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}
