using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly AdoNetDb _db;

    public DashboardController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Dashboard_GetSummary");
        var table = _db.FillDataTable(command);

        if (table.Rows.Count == 0)
        {
            return Ok(ApiResponse<object>.SuccessResult(new DashboardSummaryDto()));
        }

        var row = table.Rows[0];
        var result = new DashboardSummaryDto
        {
            TotalBuildings = row.GetIntValue("TotalBuildings"),
            TotalRooms = row.GetIntValue("TotalRooms"),
            EmptyRooms = row.GetIntValue("EmptyRooms"),
            RentedRooms = row.GetIntValue("RentedRooms"),
            MaintenanceRooms = row.GetIntValue("MaintenanceRooms"),
            TotalTenants = row.GetIntValue("TotalTenants"),
            ActiveContracts = row.GetIntValue("ActiveContracts"),
            UnpaidInvoices = row.GetIntValue("UnpaidInvoices"),
            OverdueInvoices = row.GetIntValue("OverdueInvoices"),
            TotalPaidAmount = row.GetDecimalValue("TotalPaidAmount"),
            TotalDebtAmount = row.GetDecimalValue("TotalDebtAmount")
        };

        return Ok(ApiResponse<object>.SuccessResult(result));
    }

    [HttpGet("monthly-revenue")]
    public IActionResult GetMonthlyRevenue([FromQuery] int year = 2026)
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Dashboard_GetMonthlyRevenue");
        command.Parameters.AddWithValue("@Year", year);

        var table = _db.FillDataTable(command);
        var result = table.Rows
            .Cast<DataRow>()
            .Select(row => new MonthlyRevenueDto
            {
                Month = row.GetIntValue("Month"),
                TotalInvoiceAmount = row.GetDecimalValue("TotalInvoiceAmount"),
                TotalPaidAmount = row.GetDecimalValue("TotalPaidAmount"),
                TotalDebtAmount = row.GetDecimalValue("TotalDebtAmount")
            })
            .ToList();

        return Ok(ApiResponse<object>.SuccessResult(result));
    }

    [HttpGet("room-status-statistics")]
    public IActionResult GetRoomStatusStatistics()
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Dashboard_GetRoomStatusStatistics");
        var table = _db.FillDataTable(command);

        var result = table.Rows
            .Cast<DataRow>()
            .Select(row => new RoomStatusStatsDto
            {
                Status = row.GetIntValue("Status"),
                StatusText = row.GetStringValue("StatusText"),
                Count = row.GetIntValue("Count")
            })
            .ToList();

        return Ok(ApiResponse<object>.SuccessResult(result));
    }
}
