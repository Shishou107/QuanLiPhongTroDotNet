using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.Entities;

public class InvoiceDetail
{
    public Guid Id { get; set; }

    [Required]
    public Guid InvoiceId { get; set; }

    public Guid? ServiceId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = null!;

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Invoice? Invoice { get; set; }

    public virtual Service? Service { get; set; }
}
