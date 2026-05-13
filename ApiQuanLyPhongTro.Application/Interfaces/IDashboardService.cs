using System.Collections.Generic;
using System.Threading.Tasks;
using ApiQuanLyPhongTro.Application.DTO;

namespace ApiQuanLyPhongTro.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(int year);
    Task<List<RoomStatusStatsDto>> GetRoomStatusStatisticsAsync();
}
