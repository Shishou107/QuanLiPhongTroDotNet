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

    public async Task<IActionResult> Index(int? month, int? year, int? status, int page = 1)
    {
        var result = await _invoiceService.GetAllAsync(month, year, status, page);
        ViewBag.Month = month;
        ViewBag.Year = year;
        ViewBag.Status = status;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var activeContracts = await _contractService.GetAllAsync(status: 1, pageSize: 100); // Active
        ViewBag.Contracts = new SelectList(activeContracts.Items.Select(c => new { 
            Id = c.Id, 
            DisplayName = $"{c.RoomNumber} - {c.TenantName}" 
        }), "Id", "DisplayName");

        var services = await _servicesService.GetAllAsync();
        ViewBag.Services = new SelectList(services.Select(s => new {
            Id = s.Id,
            DisplayName = $"{s.Name} ({s.Unit}) - {s.DefaultPrice:N0}"
        }), "Id", "DisplayName");

        return View(new InvoiceCreateViewModel
        {
            Details = new List<InvoiceDetailInputViewModel> { new() }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvoiceCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _invoiceService.CreateAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Lập hóa đơn thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi lập hóa đơn";
        }
        var activeContracts = await _contractService.GetAllAsync(status: 1, pageSize: 100);
        ViewBag.Contracts = new SelectList(activeContracts.Items.Select(c => new { 
            Id = c.Id, 
            DisplayName = $"{c.RoomNumber} - {c.TenantName}" 
        }), "Id", "DisplayName", model.ContractId);

        var services = await _servicesService.GetAllAsync();
        ViewBag.Services = new SelectList(services.Select(s => new {
            Id = s.Id,
            DisplayName = $"{s.Name} ({s.Unit}) - {s.DefaultPrice:N0}"
        }), "Id", "DisplayName", model.Details.FirstOrDefault()?.ServiceId);
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

        var model = new InvoiceEditViewModel
        {
            Id = invoice.Id,
            ContractId = invoice.Contract?.Id ?? Guid.Empty,
            BillingMonth = invoice.BillingMonth,
            BillingYear = invoice.BillingYear,
            DueDate = invoice.DueDate,
            Status = invoice.Status,
            Details = invoice.Details.Select(d => new InvoiceDetailInputViewModel
            {
                ServiceId = d.ServiceId ?? Guid.Empty,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            }).ToList()
        };

        var activeContracts = await _contractService.GetAllAsync(pageSize: 100);
        ViewBag.Contracts = new SelectList(activeContracts.Items.Select(c => new { 
            Id = c.Id, 
            DisplayName = $"{c.RoomNumber} - {c.TenantName}" 
        }), "Id", "DisplayName", model.ContractId);

        var services = await _servicesService.GetAllAsync();
        ViewBag.Services = new SelectList(services.Select(s => new {
            Id = s.Id,
            DisplayName = $"{s.Name} ({s.Unit}) - {s.DefaultPrice:N0}"
        }), "Id", "DisplayName");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(InvoiceEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _invoiceService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cập nhật hóa đơn thành công";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật hóa đơn";
        }
        
        var activeContracts = await _contractService.GetAllAsync(pageSize: 100);
        ViewBag.Contracts = new SelectList(activeContracts.Items.Select(c => new { 
            Id = c.Id, 
            DisplayName = $"{c.RoomNumber} - {c.TenantName}" 
        }), "Id", "DisplayName", model.ContractId);

        var services = await _servicesService.GetAllAsync();
        ViewBag.Services = new SelectList(services.Select(s => new {
            Id = s.Id,
            DisplayName = $"{s.Name} ({s.Unit}) - {s.DefaultPrice:N0}"
        }), "Id", "DisplayName");

        return View(model);
    }
}
