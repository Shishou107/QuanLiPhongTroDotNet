using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class ContractsController : Controller
{
    private readonly ContractService _contractService;
    private readonly TenantService _tenantService;
    private readonly RoomService _roomService;

    public ContractsController(ContractService contractService, TenantService tenantService, RoomService roomService)
    {
        _contractService = contractService;
        _tenantService = tenantService;
        _roomService = roomService;
    }

    public async Task<IActionResult> Index(string keyword, int? status, int page = 1)
    {
        var result = await _contractService.GetAllAsync(keyword, status, page);
        ViewBag.Status = status;
        ViewBag.Keyword = keyword;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var tenants = await _tenantService.GetAllAsync(pageSize: 100);
        var rooms = await _roomService.GetAllAsync(status: 0, pageSize: 100); // Only empty rooms

        ViewBag.Tenants = new SelectList(tenants.Items, "Id", "FullName");
        ViewBag.Rooms = new SelectList(rooms.Items, "Id", "RoomNumber");
        return View(new ContractCreateViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContractCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _contractService.CreateContractAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Tạo hợp đồng thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi tạo hợp đồng";
        }
        var tenants = await _tenantService.GetAllAsync(pageSize: 100);
        var rooms = await _roomService.GetAllAsync(status: 0, pageSize: 100);
        ViewBag.Tenants = new SelectList(tenants.Items, "Id", "FullName", model.TenantId);
        ViewBag.Rooms = new SelectList(rooms.Items, "Id", "RoomNumber", model.RoomId);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return View(contract);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();

        var model = new ContractEditViewModel
        {
            Id = contract.Id,
            TenantId = contract.Tenant.Id,
            RoomId = contract.Room.Id,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            RentPrice = contract.RentPrice,
            DepositAmount = contract.DepositAmount,
            Status = contract.Status
        };

        var tenants = await _tenantService.GetAllAsync(pageSize: 100);
        var rooms = await _roomService.GetAllAsync(pageSize: 100); // Show all rooms for editing

        ViewBag.Tenants = new SelectList(tenants.Items, "Id", "FullName", model.TenantId);
        ViewBag.Rooms = new SelectList(rooms.Items, "Id", "RoomNumber", model.RoomId);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ContractEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _contractService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cập nhật hợp đồng thành công";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật hợp đồng";
        }

        var tenants = await _tenantService.GetAllAsync(pageSize: 100);
        var rooms = await _roomService.GetAllAsync(pageSize: 100);
        ViewBag.Tenants = new SelectList(tenants.Items, "Id", "FullName", model.TenantId);
        ViewBag.Rooms = new SelectList(rooms.Items, "Id", "RoomNumber", model.RoomId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Print(Guid id)
    {
        var model = await _contractService.GetByIdAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }
}
