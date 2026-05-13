using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class TenantListViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string IdentityNumber { get; set; } = null!;
    public string? Address { get; set; }
    public string? CurrentRoom { get; set; }
    public string? CurrentBuilding { get; set; }
    public string? StatusText { get; set; }

}

public class TenantDetailViewModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string IdentityNumber { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public ContractListViewModel? CurrentContract { get; set; }
    public List<ContractListViewModel> Contracts { get; set; } = new();
    public List<InvoiceListViewModel> Invoices { get; set; } = new();
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
}

public class TenantCreateViewModel
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    public string PhoneNumber { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Số CCCD không được để trống")]
    [Display(Name = "Số CCCD/CMND")]
    public string IdentityNumber { get; set; } = null!;

    [Display(Name = "Ngày sinh")]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Địa chỉ thường trú")]
    public string? Address { get; set; }
}

public class TenantEditViewModel : TenantCreateViewModel
{
    public Guid Id { get; set; }
}
