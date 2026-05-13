using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class ServiceListViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal DefaultPrice { get; set; }
}

public class ServiceCreateViewModel
{
    [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
    [Display(Name = "Tên dịch vụ")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Đơn vị tính không được để trống")]
    [Display(Name = "Đơn vị tính")]
    public string Unit { get; set; } = null!;

    [Required(ErrorMessage = "Đơn giá không được để trống")]
    [Display(Name = "Đơn giá")]
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải từ 0 trở lên")]
    public decimal DefaultPrice { get; set; }
}

public class ServiceEditViewModel : ServiceCreateViewModel
{
    public Guid Id { get; set; }
}
