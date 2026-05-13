using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class ContractListViewModel
{
    public Guid Id { get; set; }
    public string ContractCode { get; set; } = null!;
    public string TenantName { get; set; } = null!;
    public string RoomNumber { get; set; } = null!;
    public string BuildingName { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public int Status { get; set; }
}

public class ContractDetailViewModel
{
    public Guid Id { get; set; }
    public string ContractCode { get; set; } = null!;
    public TenantListViewModel Tenant { get; set; } = null!;
    public RoomListViewModel Room { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public int Status { get; set; }
    public List<InvoiceListViewModel> Invoices { get; set; } = new();
    public List<PaymentListViewModel> Payments { get; set; } = new();
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
}

public class ContractCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn khách thuê")]
    [Display(Name = "Khách thuê")]
    public Guid TenantId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phòng")]
    [Display(Name = "Phòng")]
    public Guid RoomId { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    [Display(Name = "Ngày bắt đầu")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
    [Display(Name = "Ngày kết thúc")]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(6));

    [Required(ErrorMessage = "Giá thuê không được để trống")]
    [Display(Name = "Giá thuê")]
    public decimal RentPrice { get; set; }

    [Display(Name = "Tiền cọc")]
    public decimal DepositAmount { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public class ContractEditViewModel : ContractCreateViewModel
{
    public Guid Id { get; set; }
    public int Status { get; set; }
}
