using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class InvoiceListViewModel
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = null!;
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public string TenantName { get; set; } = null!;
    public string RoomNumber { get; set; } = null!;
    public string BuildingName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount => TotalAmount - PaidAmount;
    public DateOnly? DueDate { get; set; }
    public int Status { get; set; }
}

public class InvoiceDetailViewModel
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = null!;
    public int BillingMonth { get; set; }
    public int BillingYear { get; set; }
    public ContractListViewModel? Contract { get; set; }
    public TenantListViewModel Tenant { get; set; } = null!;
    public RoomListViewModel Room { get; set; } = null!;

    public List<InvoiceDetailItemViewModel> Details { get; set; } = new();
    public List<PaymentListViewModel> Payments { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DebtAmount => TotalAmount - PaidAmount;
    public DateOnly? DueDate { get; set; }
    public int Status { get; set; }
}

public class InvoiceDetailItemViewModel
{
    public Guid Id { get; set; }
    public Guid? ServiceId { get; set; }
    public string ServiceName { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class InvoiceCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn hợp đồng")]
    [Display(Name = "Hợp đồng")]
    public Guid ContractId { get; set; }

    [Required]
    [Display(Name = "Tháng")]
    [Range(1, 12)]
    public int BillingMonth { get; set; } = DateTime.Today.Month;

    [Required]
    [Display(Name = "Năm")]
    public int BillingYear { get; set; } = DateTime.Today.Year;

    [Display(Name = "Hạn thanh toán")]
    public DateOnly? DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

    public List<InvoiceDetailInputViewModel> Details { get; set; } = new();
}

public class InvoiceDetailInputViewModel
{
    public Guid ServiceId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}

public class InvoiceEditViewModel : InvoiceCreateViewModel
{
    public Guid Id { get; set; }
    public int Status { get; set; }
}
