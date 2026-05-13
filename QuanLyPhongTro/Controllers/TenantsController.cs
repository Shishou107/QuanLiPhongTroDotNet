using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class TenantsController : Controller
{
    private readonly TenantService _tenantService;

    public TenantsController(TenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async Task<IActionResult> Index(string keyword, int page = 1)
    {
        var result = await _tenantService.GetAllAsync(keyword, page);
        ViewBag.Keyword = keyword;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;
        return View(result.Items);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(TenantCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _tenantService.CreateAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Thêm khách thuê thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi tạo khách thuê";
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _tenantService.GetByIdAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TenantEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _tenantService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cập nhật khách thuê thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật";
        }
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var tenant = await _tenantService.GetDetailsAsync(id);
        if (tenant == null) return NotFound();
        return View(tenant);
    }
}
