using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class DashboardService
{
    private readonly BaseApiService _apiService;

    public DashboardService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<DashboardViewModel?> GetDashboardDataAsync()
    {
        var summaryResponse = await _apiService.GetAsync<DashboardViewModel>("dashboard/summary");
        if (summaryResponse == null || !summaryResponse.Success || summaryResponse.Data == null)
            return null;

        var model = summaryResponse.Data;

        // Fetch monthly revenue
        var revenueResponse = await _apiService.GetAsync<List<MonthlyRevenueItem>>("dashboard/monthly-revenue");
        if (revenueResponse != null && revenueResponse.Success && revenueResponse.Data != null)
        {
            model.MonthlyRevenueItems = revenueResponse.Data;
        }

        // Fetch room status stats
        var statusResponse = await _apiService.GetAsync<List<RoomStatusItem>>("dashboard/room-status-statistics");
        if (statusResponse != null && statusResponse.Success && statusResponse.Data != null)
        {
            model.RoomStatusItems = statusResponse.Data;
        }

        // Fetch recent invoices (paginated)
        var invoicesResponse = await _apiService.GetAsync<PaginationResult<InvoiceListViewModel>>("invoices?pageSize=5");
        if (invoicesResponse != null && invoicesResponse.Success && invoicesResponse.Data != null)
        {
            model.RecentInvoices = invoicesResponse.Data.Items.ToList();
        }

        // Fetch available rooms
        var roomsResponse = await _apiService.GetAsync<List<RoomListViewModel>>("rooms/available");
        if (roomsResponse != null && roomsResponse.Success && roomsResponse.Data != null)
        {
            model.AvailableRooms = roomsResponse.Data;
        }

        return model;
    }
}
