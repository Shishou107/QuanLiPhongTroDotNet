using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServicesController : ControllerBase
{
    private readonly AdoNetDb _db;

    public ServicesController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Services_GetAll");
        var table = _db.FillDataTable(command);

        var services = table.Rows
            .Cast<DataRow>()
            .Select(MapService)
            .ToList();

        return Ok(ApiResponse<object>.SuccessResult(services));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Services_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        var table = _db.FillDataTable(command);

        if (table.Rows.Count == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay dich vu"));
        }

        return Ok(ApiResponse<object>.SuccessResult(MapService(table.Rows[0])));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Services_Create");

        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = dto.Name;
        command.Parameters.Add("@Unit", SqlDbType.NVarChar, 50).Value = dto.Unit;
        command.Parameters.Add("@DefaultPrice", SqlDbType.Decimal).Value = dto.DefaultPrice;
        var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
        idParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();

        var id = (Guid)idParameter.Value;
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.SuccessResult(id, "Tao dich vu thanh cong"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateServiceDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Services_Update");

        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = dto.Name;
        command.Parameters.Add("@Unit", SqlDbType.NVarChar, 50).Value = dto.Unit;
        command.Parameters.Add("@DefaultPrice", SqlDbType.Decimal).Value = dto.DefaultPrice;

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay dich vu"));
        }

        return Ok(ApiResponse.SuccessResult("Cap nhat dich vu thanh cong"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Services_Delete");
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay dich vu"));
            }

            return Ok(ApiResponse.SuccessResult("Da xoa dich vu"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    private static ServiceDto MapService(DataRow row)
    {
        return new ServiceDto
        {
            Id = row.GetGuidValue("Id"),
            Name = row.GetStringValue("Name"),
            Unit = row.GetStringValue("Unit"),
            DefaultPrice = row.GetDecimalValue("DefaultPrice"),
            IsActive = true
        };
    }
}
