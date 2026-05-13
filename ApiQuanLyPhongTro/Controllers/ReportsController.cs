using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReportsController : ControllerBase
{
    private readonly AdoNetDb _db;

    public ReportsController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet("revenue-by-month")]
    public IActionResult GetRevenueByMonth([FromQuery] int year = 2026)
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Reports_RevenueByMonth");
        command.Parameters.Add("@Year", SqlDbType.Int).Value = year;

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

    [HttpGet("debt-by-tenant")]
    public IActionResult GetDebtByTenant()
    {
        return ExecuteObjectReport("sp_Reports_DebtByTenant");
    }

    [HttpGet("room-occupancy")]
    public IActionResult GetRoomOccupancy()
    {
        return ExecuteObjectReport("sp_Reports_RoomOccupancy");
    }

    [HttpGet("service-usage")]
    public IActionResult GetServiceUsage([FromQuery] int month, [FromQuery] int year)
    {
        return ExecuteObjectReport("sp_Reports_ServiceUsage", command =>
        {
            command.Parameters.Add("@Month", SqlDbType.Int).Value = month;
            command.Parameters.Add("@Year", SqlDbType.Int).Value = year;
        });
    }

    private IActionResult ExecuteObjectReport(string procedureName, Action<SqlCommand>? configure = null)
    {
        var rows = new List<Dictionary<string, object?>>();

        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, procedureName);
        configure?.Invoke(command);

        var table = _db.FillDataTable(command);

        foreach (DataRow tableRow in table.Rows)
        {
            var row = new Dictionary<string, object?>();
            foreach (DataColumn column in table.Columns)
            {
                row[column.ColumnName] = tableRow.IsNull(column) ? null : tableRow[column];
            }

            rows.Add(row);
        }

        return Ok(ApiResponse<object>.SuccessResult(rows));
    }
}
