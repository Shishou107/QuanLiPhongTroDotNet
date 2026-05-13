using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class RoomListViewModel
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; } = null!;
    public string BuildingName { get; set; } = null!;
    public decimal? Area { get; set; }
    public decimal RentPrice { get; set; }
    public int Status { get; set; }
    public string? CurrentTenantName { get; set; }
}

public class RoomDetailViewModel
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public decimal? Area { get; set; }
    public decimal RentPrice { get; set; }
    public int Status { get; set; }
    public string? Description { get; set; }
    public ContractListViewModel? CurrentContract { get; set; }
    public List<InvoiceListViewModel> RecentInvoices { get; set; } = new();
}

public class RoomCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn tòa nhà")]
    [Display(Name = "Tòa nhà")]
    public Guid BuildingId { get; set; }

    [Required(ErrorMessage = "Số phòng không được để trống")]
    [Display(Name = "Số phòng")]
    public string RoomNumber { get; set; } = null!;

    [Display(Name = "Diện tích (m2)")]
    [Range(0.1, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn 0")]
    public decimal? Area { get; set; }

    [Required(ErrorMessage = "Giá thuê không được để trống")]
    [Display(Name = "Giá thuê cơ bản")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá thuê phải từ 0 trở lên")]
    public decimal BasePrice { get; set; }

    [Display(Name = "Trạng thái")]
    public int Status { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}

public class RoomEditViewModel : RoomCreateViewModel
{
    public Guid Id { get; set; }
    public ContractListViewModel? CurrentContract { get; set; }
}
