using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.ViewModels;

public class PaymentListViewModel
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; } = null!;
    public string TenantName { get; set; } = null!;
    public string RoomNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Method { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? Note { get; set; }
}

public class PaymentCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn hóa đơn")]
    [Display(Name = "Hóa đơn")]
    public Guid InvoiceId { get; set; }

    [Required(ErrorMessage = "Số tiền không được để trống")]
    [Display(Name = "Số tiền thanh toán")]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phương thức")]
    [Display(Name = "Phương thức thanh toán")]
    public int PaymentMethod { get; set; }

    [Display(Name = "Ngày thanh toán")]
    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }
}

public class PaymentEditViewModel : PaymentCreateViewModel
{
    public Guid Id { get; set; }
}
