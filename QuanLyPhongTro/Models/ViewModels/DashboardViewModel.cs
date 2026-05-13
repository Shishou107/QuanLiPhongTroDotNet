using System.Collections.Generic;

namespace QuanLyPhongTro.Models.ViewModels;

public class DashboardViewModel
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
    
    public List<MonthlyRevenueItem> MonthlyRevenueItems { get; set; } = new();
    public List<RoomStatusItem> RoomStatusItems { get; set; } = new();
    public List<InvoiceListViewModel> RecentInvoices { get; set; } = new();
    public List<RoomListViewModel> AvailableRooms { get; set; } = new();
}

public class MonthlyRevenueItem
{
    public int Month { get; set; }
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
    public decimal Amount { get; internal set; }
}

public class RoomStatusItem
{
    public int Status { get; set; }
    public string StatusText { get; set; } = null!;
    public int Count { get; set; }
}
