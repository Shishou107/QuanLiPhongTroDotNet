using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Application.DTO;

public class DashboardSummaryDto
{
    public int TotalBuildings { get; set; }
    public int TotalRooms { get; set; }
    public int EmptyRooms { get; set; }
    public int RentedRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveContracts { get; set; }
    public int UnpaidInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
}

public class MonthlyRevenueDto
{
    public int Month { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
}

public class RoomStatusStatsDto
{
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int Count { get; set; }
}
