using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.Entities;

public class Service
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Đơn vị tính không được để trống")]
    [MaxLength(50)]
    public string Unit { get; set; } = null!;

    [Required(ErrorMessage = "Đơn giá không được để trống")]
    public decimal UnitPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
}
