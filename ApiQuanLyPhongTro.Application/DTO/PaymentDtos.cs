using System;

namespace ApiQuanLyPhongTro.Application.DTO;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string? InvoiceCode { get; set; }
    public string? TenantName { get; set; }
    public string? RoomNumber { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string? Note { get; set; }
}

public class CreatePaymentDto
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash"; // Cash, BankTransfer, Momo, ZaloPay
    public DateTime? PaymentDate { get; set; }
    public string? Note { get; set; }
}
