using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class BuildingsController : Controller
{
    private readonly BuildingService _buildingService;

    public BuildingsController(BuildingService buildingService)
    {
        _buildingService = buildingService;
    }

    public async Task<IActionResult> Index(string keyword)
    {
        var buildings = await _buildingService.GetAllAsync(keyword);
        ViewBag.Keyword = keyword;
        return View(buildings);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(BuildingCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _buildingService.CreateAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Thêm tòa nhà thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi tạo tòa nhà";
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _buildingService.GetByIdAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(BuildingEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _buildingService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cập nhật tòa nhà thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật";
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _buildingService.DeleteAsync(id);
        return Json(new { success = result?.Success ?? false, message = result?.Message });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var building = await _buildingService.GetDetailsAsync(id);
        if (building == null) return NotFound();
        return View(building);
    }
}
