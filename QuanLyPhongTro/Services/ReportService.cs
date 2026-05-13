using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class ReportService
{
    private readonly BaseApiService _apiService;

    public ReportService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<MonthlyRevenueItem>> GetRevenueByMonthAsync()
    {
        var response = await _apiService.GetAsync<List<MonthlyRevenueItem>>("reports/revenue-by-month");
        return response?.Data ?? new List<MonthlyRevenueItem>();
    }

    public async Task<List<ReportOccupancyViewModel>> GetRoomOccupancyAsync()
    {
        var response = await _apiService.GetAsync<List<ReportOccupancyViewModel>>("reports/room-occupancy");
        return response?.Data ?? new List<ReportOccupancyViewModel>();
    }
}
