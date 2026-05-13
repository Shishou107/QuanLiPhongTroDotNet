using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.Entities;

public class Tenant
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    public DateOnly? Dob { get; set; }

    [Required(ErrorMessage = "Số CCCD không được để trống")]
    [MaxLength(20)]
    public string IdCardNumber { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [MaxLength(15)]
    public string PhoneNumber { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(255)]
    public string? PermanentAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
