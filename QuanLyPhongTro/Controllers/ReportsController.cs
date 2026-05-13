using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Services;

namespace QuanLyPhongTro.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly ReportService _reportService;

    public ReportsController(ReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Revenue = await _reportService.GetRevenueByMonthAsync();
        ViewBag.Occupancy = await _reportService.GetRoomOccupancyAsync();
        return View();
    }
}
