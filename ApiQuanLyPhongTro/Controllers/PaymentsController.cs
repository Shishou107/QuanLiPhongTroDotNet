using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly AdoNetDb _db;

    public PaymentsController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? invoiceId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? roomId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? method,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var items = new List<PaymentDto>();

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Payments_GetAll");
        command.Parameters.Add("@InvoiceId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(invoiceId);
        command.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(tenantId);
        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(roomId);
        command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = SqlParameterValue.FromNullable(fromDate);
        command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = SqlParameterValue.FromNullable(toDate);
        command.Parameters.Add("@Method", SqlDbType.NVarChar, 50).Value = SqlParameterValue.FromString(method);
        command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        var totalItemsParameter = command.Parameters.Add("@TotalItems", SqlDbType.Int);
        totalItemsParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(MapPayment(reader));
        }

        await reader.CloseAsync();

        return Ok(ApiResponse<object>.SuccessResult(new PaginationResult<PaymentDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = (int)totalItemsParameter.Value
        }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Payments_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay thanh toan"));
        }

        return Ok(ApiResponse<object>.SuccessResult(MapPayment(reader)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentDto dto)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Payments_Create");
            command.Parameters.Add("@InvoiceId", SqlDbType.UniqueIdentifier).Value = dto.InvoiceId;
            command.Parameters.Add("@Amount", SqlDbType.Decimal).Value = dto.Amount;
            command.Parameters.Add("@Method", SqlDbType.NVarChar, 50).Value = dto.Method;
            command.Parameters.Add("@PaymentDate", SqlDbType.DateTime).Value = SqlParameterValue.FromNullable(dto.PaymentDate);
            command.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = SqlParameterValue.FromString(dto.Note);
            var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
            idParameter.Direction = ParameterDirection.Output;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return Ok(ApiResponse<Guid>.SuccessResult((Guid)idParameter.Value, "Them thanh toan thanh cong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Payments_Delete");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay thanh toan"));
        }

        return Ok(ApiResponse.SuccessResult("Da xoa thanh toan"));
    }

    private static PaymentDto MapPayment(SqlDataReader reader)
    {
        return new PaymentDto
        {
            Id = reader.GetGuidValue("Id"),
            InvoiceId = reader.GetGuidValue("InvoiceId"),
            InvoiceCode = reader.GetNullableStringValue("InvoiceCode"),
            TenantName = reader.GetNullableStringValue("TenantName"),
            RoomNumber = reader.GetNullableStringValue("RoomNumber"),
            Amount = reader.GetDecimalValue("Amount"),
            Method = reader.GetNullableStringValue("Method") ?? string.Empty,
            PaymentDate = reader.GetNullableDateTimeValue("PaymentDate"),
            Note = reader.GetNullableStringValue("Note")
        };
    }
}
