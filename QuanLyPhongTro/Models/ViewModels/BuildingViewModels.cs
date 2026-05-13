using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class BuildingListViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public int TotalRooms { get; set; }
    public int EmptyRooms { get; set; }
    public int RentedRooms { get; set; }
    public int MaintenanceRooms { get; set; }
}

public class BuildingDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public List<RoomListViewModel> Rooms { get; set; } = new();
}

public class BuildingCreateViewModel
{
    [Required(ErrorMessage = "Tên tòa nhà không được để trống")]
    [Display(Name = "Tên tòa nhà")]
    public string Name { get; set; } = null!;

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}

public class BuildingEditViewModel : BuildingCreateViewModel
{
    public Guid Id { get; set; }
}
