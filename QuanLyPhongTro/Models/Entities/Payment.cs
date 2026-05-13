using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.Entities;

public class Payment
{
    public Guid Id { get; set; }

    [Required]
    public Guid InvoiceId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public int? PaymentMethod { get; set; } // 0=Cash, 1=BankTransfer, 2=Momo, 3=ZaloPay

    [MaxLength(100)]
    public string? ReferenceCode { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Invoice? Invoice { get; set; }
}
