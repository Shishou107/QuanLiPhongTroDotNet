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

    public async Task<IActionResult> Index(int page = 1)
    {
        var result = await _paymentService.GetAllAsync(page);
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? invoiceId)
    {
        var unpaidInvoices = await _invoiceService.GetAllAsync(pageSize: 100); // Filter for unpaid in real case
        ViewBag.Invoices = new SelectList(unpaidInvoices.Items.Select(i => new { 
            Id = i.Id, 
            DisplayName = $"{i.InvoiceCode} - {i.RoomNumber} - {i.TenantName}" 
        }), "Id", "DisplayName", invoiceId);
        
        return View(new PaymentCreateViewModel { InvoiceId = invoiceId ?? Guid.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
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
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(Guid id)
    {
        var model = await _paymentService.GetByIdAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }
}
