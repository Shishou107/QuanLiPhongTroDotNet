using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly PaymentService _paymentService;
    private readonly InvoiceService _invoiceService;

    public PaymentsController(PaymentService paymentService, InvoiceService invoiceService)
    {
        _paymentService = paymentService;
        _invoiceService = invoiceService;
    }

    public async Task<IActionResult> Index(string? keyword, DateTime? fromDate, DateTime? toDate, string? method, int page = 1)
    {
        var result = await _paymentService.GetAllAsync(keyword, fromDate, toDate, method, page);
        ViewBag.Keyword = keyword;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Method = method;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? invoiceId)
    {
        await PopulatePayableInvoicesAsync(invoiceId);

        var model = new PaymentCreateViewModel { InvoiceId = invoiceId ?? Guid.Empty };
        if (invoiceId.HasValue)
        {
            var invoice = await _invoiceService.GetByIdAsync(invoiceId.Value);
            if (invoice != null && invoice.DebtAmount > 0)
            {
                model.Amount = invoice.DebtAmount;
            }
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        var invoice = model.InvoiceId == Guid.Empty ? null : await _invoiceService.GetByIdAsync(model.InvoiceId);
        if (invoice == null)
        {
            ModelState.AddModelError(nameof(model.InvoiceId), "Hóa đơn không tồn tại");
        }
        else
        {
            if (invoice.TotalAmount <= 0)
            {
                ModelState.AddModelError(nameof(model.InvoiceId), "Hóa đơn chưa có tổng tiền, vui lòng cập nhật chi tiết hóa đơn trước khi thanh toán");
            }

            if (invoice.DebtAmount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Hóa đơn đã thanh toán đủ");
            }

            if (model.Amount > invoice.DebtAmount)
            {
                ModelState.AddModelError(nameof(model.Amount), $"Số tiền thanh toán không được vượt quá {invoice.DebtAmount:N0} đồng");
            }
        }

        if (ModelState.IsValid)
        {
            var result = await _paymentService.CreateAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Thanh toán thành công";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = result?.Message ?? "Lỗi khi thanh toán";
        }

        await PopulatePayableInvoicesAsync(model.InvoiceId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(Guid id)
    {
        var model = await _paymentService.GetByIdAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }

    private async Task PopulatePayableInvoicesAsync(Guid? selectedInvoiceId = null)
    {
        var invoices = await _invoiceService.GetAllAsync(pageSize: 100);
        var payableInvoices = invoices.Items
            .Where(i => i.DebtAmount > 0)
            .Select(i => new
            {
                Id = i.Id,
                DisplayName = $"{i.InvoiceCode} - {i.RoomNumber} - {i.TenantName} - còn nợ {i.DebtAmount:N0}"
            });

        ViewBag.Invoices = new SelectList(payableInvoices, "Id", "DisplayName", selectedInvoiceId);
    }
}
