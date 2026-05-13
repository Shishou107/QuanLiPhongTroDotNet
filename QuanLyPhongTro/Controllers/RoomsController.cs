using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class RoomsController : Controller
{
    private readonly RoomService _roomService;
    private readonly BuildingService _buildingService;

    public RoomsController(RoomService roomService, BuildingService buildingService)
    {
        _roomService = roomService;
        _buildingService = buildingService;
    }

    public async Task<IActionResult> Index(Guid? buildingId, int? status, string keyword, int page = 1)
    {
        var result = await _roomService.GetAllAsync(buildingId, status, keyword, page);
        var buildings = await _buildingService.GetAllAsync();

        ViewBag.Buildings = new SelectList(buildings, "Id", "Name", buildingId);
        ViewBag.BuildingId = buildingId;
        ViewBag.Status = status;
        ViewBag.Keyword = keyword;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;

        return View(result.Items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var buildings = await _buildingService.GetAllAsync();
        ViewBag.Buildings = new SelectList(buildings, "Id", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoomCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _roomService.CreateAsync(model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Thêm phòng thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi tạo phòng";
        }
        var buildings = await _buildingService.GetAllAsync();
        ViewBag.Buildings = new SelectList(buildings, "Id", "Name", model.BuildingId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _roomService.GetByIdAsync(id);
        if (model == null) return NotFound();
        
        var buildings = await _buildingService.GetAllAsync();
        ViewBag.Buildings = new SelectList(buildings, "Id", "Name", model.BuildingId);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(RoomEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (model.Status == 0)
            {
                var currentRoom = await _roomService.GetDetailsAsync(model.Id);
                if (currentRoom?.CurrentContract != null)
                {
                    TempData["Error"] = "Phong dang co hop dong hieu luc, hay ket thuc hoac huy hop dong truoc khi chuyen sang Trong.";
                    model.CurrentContract = currentRoom.CurrentContract;

                    var currentBuildings = await _buildingService.GetAllAsync();
                    ViewBag.Buildings = new SelectList(currentBuildings, "Id", "Name", model.BuildingId);
                    return View(model);
                }
            }

            var result = await _roomService.UpdateAsync(model.Id, model);
            if (result != null && result.Success)
            {
                TempData["Success"] = "Cập nhật phòng thành công";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật";
        }
        var buildings = await _buildingService.GetAllAsync();
        ViewBag.Buildings = new SelectList(buildings, "Id", "Name", model.BuildingId);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var room = await _roomService.GetDetailsAsync(id);
        if (room == null) return NotFound();
        return View(room);
    }

    public async Task<IActionResult> Maintenance(Guid? buildingId, string keyword, int page = 1)
    {
        var result = await _roomService.GetAllAsync(buildingId, status: 2, keyword, page);
        var buildings = await _buildingService.GetAllAsync();

        ViewBag.Buildings = new SelectList(buildings, "Id", "Name", buildingId);
        ViewBag.BuildingId = buildingId;
        ViewBag.Keyword = keyword;
        ViewBag.CurrentPage = result.Page;
        ViewBag.TotalPages = result.TotalPages;

        return View(result.Items);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, int status)
    {
        if (status == 0)
        {
            var room = await _roomService.GetDetailsAsync(id);
            if (room?.CurrentContract != null)
            {
                TempData["Error"] = "Phong dang co hop dong hieu luc, hay ket thuc hoac huy hop dong truoc khi chuyen sang Trong.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        var result = await _roomService.UpdateStatusAsync(id, status);
        if (result != null && result.Success)
        {
            TempData["Success"] = "Cập nhật trạng thái thành công";
        }
        else
        {
            TempData["Error"] = result?.Message ?? "Lỗi khi cập nhật trạng thái";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
