using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class ServicesController : Controller
{
    private readonly ServicesService _servicesService;

    public ServicesController(ServicesService servicesService)
    {
        _servicesService = servicesService;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _servicesService.GetAllAsync();
        return View(services);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _servicesService.CreateAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Thêm dịch vụ thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi tạo dịch vụ";
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _servicesService.GetByIdAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ServiceEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _servicesService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cập nhật dịch vụ thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật dịch vụ";
        }
        return View(model);
    }
}
