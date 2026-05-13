using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Application.DTO;

public class ContractListDto
{
    public Guid Id { get; set; }
    public string? ContractCode { get; set; } // Map from Id or a custom field
    public string TenantName { get; set; } = null!;
    public string RoomNumber { get; set; } = null!;
    public string BuildingName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Hết hạn",
        1 => "Đang hiệu lực",
        2 => "Đã hủy",
        _ => "Không xác định"
    };
}

public class ContractDetailDto
{
    public Guid Id { get; set; }
    public string? ContractCode { get; set; }
    public TenantListDto Tenant { get; set; } = null!;
    public RoomListDto Room { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Hết hạn",
        1 => "Đang hiệu lực",
        2 => "Đã hủy",
        _ => "Không xác định"
    };
    public List<InvoiceListDto> Invoices { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
}

public class CreateContractDto
{
    public Guid TenantId { get; set; }
    public Guid RoomId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public string? Note { get; set; }
}

public class UpdateContractDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public int Status { get; set; }
    public string? Note { get; set; }
}
