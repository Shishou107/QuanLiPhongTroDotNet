using System.Collections.Generic;

namespace QuanLyPhongTro.Models.ViewModels;

public class ReportRevenueViewModel
{
    public int Year { get; set; }
    public List<MonthlyRevenueItem> MonthlyData { get; set; } = new();
}

public class ReportDebtViewModel
{
    public string TenantName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string RoomNumber { get; set; } = null!;
    public decimal TotalDebt { get; set; }
}

public class ReportOccupancyViewModel
{
    public string StatusText { get; set; } = null!;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ReportServiceUsageViewModel
{
    public string ServiceName { get; set; } = null!;
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
}
