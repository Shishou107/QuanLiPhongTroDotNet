using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class InvoicesController : Controller
{
    private readonly InvoiceService _invoiceService;
    private readonly ContractService _contractService;
    private readonly ServicesService _servicesService;

    public InvoicesController(InvoiceService invoiceService, ContractService contractService, ServicesService servicesService)
    {
        _invoiceService = invoiceService;
        _contractService = contractService;
        _servicesService = servicesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetContractDetails(Guid id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return Json(new { rentPrice = contract.RentPrice });
    }

    public async Task<IActionResult> Index(int? month, int? year, int? status, string? keyword, int page = 1)
    {
        var result = await _invoiceService.GetAllAsync(month, year, status, keyword, page);
        ViewBag.Month = month;
        ViewBag.Year = year;
        ViewBag.Status = status;
        ViewBag.Keyword = keyword;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateInvoiceLookupsAsync();

        var today = DateTime.Today;
        var nextMonth = today.AddMonths(1);
        var dueDate = new DateOnly(nextMonth.Year, nextMonth.Month, 10);

        return View(new InvoiceCreateViewModel
        {
            BillingMonth = today.Month,
            BillingYear = today.Year,
            DueDate = dueDate,
            Details = await BuildInvoiceDetailsAsync(Array.Empty<InvoiceDetailInputViewModel>())
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvoiceCreateViewModel model)
    {
        var submittedDetails = model.Details ?? new List<InvoiceDetailInputViewModel>();
        var billableDetails = submittedDetails.Where(d => d.Quantity > 0).ToList();
        ValidateInvoiceDetails(billableDetails);

        if (ModelState.IsValid)
        {
            var request = new InvoiceCreateViewModel
            {
                ContractId = model.ContractId,
                BillingMonth = model.BillingMonth,
                BillingYear = model.BillingYear,
                DueDate = model.DueDate,
                Details = billableDetails
            };

            var result = await _invoiceService.CreateAsync(request);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Lap hoa don thanh cong";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "Loi khi lap hoa don");
        }

        model.Details = await BuildInvoiceDetailsAsync(submittedDetails);
        await PopulateInvoiceLookupsAsync(model.ContractId);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();
        return View(invoice);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();

        var allServices = await _servicesService.GetAllAsync();
        var model = new InvoiceEditViewModel
        {
            Id = invoice.Id,
            ContractId = invoice.Contract?.Id ?? Guid.Empty,
            BillingMonth = invoice.BillingMonth,
            BillingYear = invoice.BillingYear,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            Details = allServices
                .Where(s => !IsOtherService(s.Name))
                .Select(s =>
                {
                    var existing = invoice.Details.FirstOrDefault(d => d.ServiceId == s.Id);
                    return new InvoiceDetailInputViewModel
                    {
                        ServiceId = s.Id,
                        ServiceName = s.Name,
                        Quantity = existing?.Quantity ?? 0,
                        UnitPrice = existing?.UnitPrice ?? s.DefaultPrice
                    };
                }).ToList()
        };

        await PopulateInvoiceLookupsAsync(model.ContractId, includeInactiveContracts: true);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(InvoiceEditViewModel model)
    {
        model.Details = model.Details.Where(d => d.Quantity > 0).ToList();
        ValidateInvoiceDetails(model.Details);

        if (ModelState.IsValid)
        {
            var result = await _invoiceService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cap nhat hoa don thanh cong";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }

            ModelState.AddModelError(string.Empty, result?.Message ?? "Loi khi cap nhat hoa don");
        }

        await PopulateInvoiceLookupsAsync(model.ContractId, includeInactiveContracts: true);
        return View(model);
    }

    private async Task PopulateInvoiceLookupsAsync(Guid? selectedContractId = null, bool includeInactiveContracts = false)
    {
        var contracts = includeInactiveContracts
            ? await _contractService.GetAllAsync(pageSize: 100)
            : await _contractService.GetAllAsync(status: 1, pageSize: 100);

        ViewBag.Contracts = new SelectList(contracts.Items.Select(c => new
        {
            Id = c.Id,
            DisplayName = $"{c.RoomNumber} - {c.TenantName}"
        }), "Id", "DisplayName", selectedContractId);

        var services = await _servicesService.GetAllAsync();
        ViewBag.Services = new SelectList(services.Select(s => new
        {
            Id = s.Id,
            DisplayName = $"{s.Name} ({s.Unit}) - {s.DefaultPrice:N0}"
        }), "Id", "DisplayName");
        ViewBag.ServicePrices = JsonSerializer.Serialize(services.ToDictionary(s => s.Id.ToString(), s => s.DefaultPrice));
    }

    private async Task<List<InvoiceDetailInputViewModel>> BuildInvoiceDetailsAsync(IEnumerable<InvoiceDetailInputViewModel> currentDetails)
    {
        var currentByService = currentDetails
            .Where(d => d.ServiceId != Guid.Empty)
            .GroupBy(d => d.ServiceId)
            .ToDictionary(g => g.Key, g => g.First());

        var services = await _servicesService.GetAllAsync();

        return services
            .Where(s => !IsOtherService(s.Name))
            .Select(s =>
            {
                currentByService.TryGetValue(s.Id, out var current);
                return new InvoiceDetailInputViewModel
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Quantity = current?.Quantity ?? 0,
                    UnitPrice = current?.UnitPrice ?? s.DefaultPrice,
                    Note = current?.Note
                };
            })
            .ToList();
    }

    private static bool IsOtherService(string name)
    {
        var normalized = RemoveVietnameseMarks(name).ToLowerInvariant();
        return normalized.Contains("dich vu khac") || normalized.Contains("khac");
    }

    private void ValidateInvoiceDetails(IReadOnlyCollection<InvoiceDetailInputViewModel> details)
    {
        if (!details.Any())
        {
            ModelState.AddModelError(string.Empty, "Hoa don phai co it nhat mot khoan thu. Vui long nhap so luong lon hon 0 cho mot dich vu.");
            return;
        }

        for (var i = 0; i < details.Count; i++)
        {
            var detail = details.ElementAt(i);
            if (detail.ServiceId == Guid.Empty)
            {
                ModelState.AddModelError($"Details[{i}].ServiceId", "Vui long chon dich vu");
            }

            if (detail.Quantity <= 0)
            {
                ModelState.AddModelError($"Details[{i}].Quantity", "So luong phai lon hon 0");
            }
        }
    }

    private static string RemoveVietnameseMarks(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray();
        return new string(chars).Normalize(System.Text.NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }
}
