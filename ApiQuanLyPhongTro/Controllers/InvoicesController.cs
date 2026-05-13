using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly AdoNetDb _db;

    public InvoicesController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? keyword,
        [FromQuery] Guid? contractId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? roomId,
        [FromQuery] Guid? buildingId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] int? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var items = new List<InvoiceListDto>();

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_GetAll");
        command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = SqlParameterValue.FromString(keyword);
        command.Parameters.Add("@ContractId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(contractId);
        command.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(tenantId);
        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(roomId);
        command.Parameters.Add("@BuildingId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(buildingId);
        command.Parameters.Add("@Month", SqlDbType.Int).Value = SqlParameterValue.FromNullable(month);
        command.Parameters.Add("@Year", SqlDbType.Int).Value = SqlParameterValue.FromNullable(year);
        command.Parameters.Add("@Status", SqlDbType.Int).Value = SqlParameterValue.FromNullable(status);
        command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        var totalItemsParameter = command.Parameters.Add("@TotalItems", SqlDbType.Int);
        totalItemsParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(MapInvoiceList(reader));
        }

        await reader.CloseAsync();

        return Ok(ApiResponse<object>.SuccessResult(new PaginationResult<InvoiceListDto>
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
        InvoiceDetailDto? invoice = null;

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            invoice = new InvoiceDetailDto
            {
                Id = reader.GetGuidValue("Id"),
                InvoiceCode = reader.GetNullableStringValue("InvoiceCode"),
                BillingMonth = reader.GetIntValue("BillingMonth"),
                BillingYear = reader.GetIntValue("BillingYear"),
                TotalAmount = reader.GetDecimalValue("TotalAmount"),
                PaidAmount = reader.GetDecimalValue("PaidAmount"),
                DueDate = reader.GetNullableDateOnlyValue("DueDate"),
                Status = reader.GetIntValue("Status"),
                Contract = new ContractListDto
                {
                    Id = reader.GetGuidValue("ContractId"),
                    RentPrice = reader.GetDecimalValue("ContractRentPrice")
                },
                Tenant = new TenantListDto
                {
                    Id = reader.GetGuidValue("TenantId"),
                    FullName = reader.GetStringValue("TenantName"),
                    PhoneNumber = reader.GetNullableStringValue("TenantPhoneNumber") ?? string.Empty
                },
                Room = new RoomListDto
                {
                    Id = reader.GetGuidValue("RoomId"),
                    RoomNumber = reader.GetStringValue("RoomNumber"),
                    BuildingName = reader.GetNullableStringValue("BuildingName") ?? string.Empty
                }
            };
        }

        if (invoice == null)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay hoa don"));
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                invoice.Details.Add(new InvoiceDetailItemDto
                {
                    Id = reader.GetGuidValue("Id"),
                    ServiceId = reader.GetNullableGuidValue("ServiceId"),
                    ServiceName = reader.GetStringValue("ServiceName"),
                    Quantity = reader.GetDecimalValue("Quantity"),
                    UnitPrice = reader.GetDecimalValue("UnitPrice"),
                    Amount = reader.GetDecimalValue("Amount"),
                    Note = reader.GetNullableStringValue("Note")
                });
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                invoice.Payments.Add(new PaymentDto
                {
                    Id = reader.GetGuidValue("Id"),
                    InvoiceId = invoice.Id,
                    Amount = reader.GetDecimalValue("Amount"),
                    PaymentDate = reader.GetNullableDateTimeValue("PaymentDate"),
                    Method = reader.GetNullableStringValue("Method") ?? string.Empty,
                    Note = reader.GetNullableStringValue("Note")
                });
            }
        }

        return Ok(ApiResponse<object>.SuccessResult(invoice));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInvoiceDto dto)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_Create");
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.Add("@ContractId", SqlDbType.UniqueIdentifier).Value = dto.ContractId;
            command.Parameters.Add("@BillingMonth", SqlDbType.Int).Value = dto.BillingMonth;
            command.Parameters.Add("@BillingYear", SqlDbType.Int).Value = dto.BillingYear;
            command.Parameters.Add("@DueDate", SqlDbType.Date).Value = dto.DueDate.HasValue ? dto.DueDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
            var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
            idParameter.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync();
            var invoiceId = (Guid)idParameter.Value;

            foreach (var detail in dto.Details)
            {
                await using var detailCommand = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_AddDetail");
                detailCommand.Transaction = (SqlTransaction)transaction;
                AddInvoiceDetailParameters(detailCommand, invoiceId, detail.ServiceId, detail.Quantity, detail.UnitPrice, detail.Note);
                await detailCommand.ExecuteNonQueryAsync();
            }

            await using var recalculateCommand = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_Recalculate");
            recalculateCommand.Transaction = (SqlTransaction)transaction;
            recalculateCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = invoiceId;
            await recalculateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return CreatedAtAction(nameof(GetById), new { id = invoiceId }, ApiResponse<Guid>.SuccessResult(invoiceId, "Tao hoa don thanh cong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateInvoiceDto dto)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_Update");
            command.Transaction = (SqlTransaction)transaction;
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
            command.Parameters.Add("@BillingMonth", SqlDbType.Int).Value = dto.BillingMonth;
            command.Parameters.Add("@BillingYear", SqlDbType.Int).Value = dto.BillingYear;
            command.Parameters.Add("@DueDate", SqlDbType.Date).Value = dto.DueDate.HasValue ? dto.DueDate.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
            command.Parameters.Add("@Status", SqlDbType.Int).Value = dto.Status;
            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return NotFound(ApiResponse.FailureResult("Khong tim thay hoa don"));
            }

            await using var clearCommand = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_ClearDetails");
            clearCommand.Transaction = (SqlTransaction)transaction;
            clearCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
            await clearCommand.ExecuteNonQueryAsync();

            foreach (var detail in dto.Details)
            {
                await using var detailCommand = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_AddDetail");
                detailCommand.Transaction = (SqlTransaction)transaction;
                AddInvoiceDetailParameters(detailCommand, id, detail.ServiceId, detail.Quantity, detail.UnitPrice, detail.Note);
                await detailCommand.ExecuteNonQueryAsync();
            }

            await using var recalculateCommand = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_Recalculate");
            recalculateCommand.Transaction = (SqlTransaction)transaction;
            recalculateCommand.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
            await recalculateCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return Ok(ApiResponse.SuccessResult("Cap nhat hoa don thanh cong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    [HttpPatch("{id}/recalculate")]
    public async Task<IActionResult> Recalculate(Guid id)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_Recalculate");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay hoa don"));
        }

        return Ok(ApiResponse.SuccessResult("Da cap nhat lai hoa don"));
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue()
    {
        return await GetByStatusList("sp_Invoices_GetOverdue");
    }

    [HttpGet("unpaid")]
    public async Task<IActionResult> GetUnpaid()
    {
        return await GetByStatusList("sp_Invoices_GetUnpaid");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Invoices_Delete");
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay hoa don"));
            }

            return Ok(ApiResponse.SuccessResult("Da xoa hoa don"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    private async Task<IActionResult> GetByStatusList(string procedureName)
    {
        var invoices = new List<InvoiceListDto>();
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, procedureName);

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            invoices.Add(MapInvoiceList(reader));
        }

        return Ok(ApiResponse<object>.SuccessResult(invoices));
    }

    private static InvoiceListDto MapInvoiceList(SqlDataReader reader)
    {
        return new InvoiceListDto
        {
            Id = reader.GetGuidValue("Id"),
            InvoiceCode = reader.GetNullableStringValue("InvoiceCode"),
            BillingMonth = reader.GetIntValue("BillingMonth"),
            BillingYear = reader.GetIntValue("BillingYear"),
            TenantName = reader.GetNullableStringValue("TenantName") ?? string.Empty,
            RoomNumber = reader.GetNullableStringValue("RoomNumber") ?? string.Empty,
            BuildingName = reader.GetNullableStringValue("BuildingName") ?? string.Empty,
            TotalAmount = reader.GetDecimalValue("TotalAmount"),
            PaidAmount = reader.GetDecimalValue("PaidAmount"),
            DueDate = reader.GetNullableDateOnlyValue("DueDate"),
            Status = reader.GetIntValue("Status")
        };
    }

    private static void AddInvoiceDetailParameters(SqlCommand command, Guid invoiceId, Guid serviceId, decimal quantity, decimal unitPrice, string? note)
    {
        command.Parameters.Add("@InvoiceId", SqlDbType.UniqueIdentifier).Value = invoiceId;
        command.Parameters.Add("@ServiceId", SqlDbType.UniqueIdentifier).Value = serviceId;
        command.Parameters.Add("@Quantity", SqlDbType.Decimal).Value = quantity;
        command.Parameters.Add("@UnitPrice", SqlDbType.Decimal).Value = unitPrice;
        command.Parameters.Add("@Note", SqlDbType.NVarChar, 255).Value = SqlParameterValue.FromString(note);
    }
}
