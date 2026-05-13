using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ContractsController : ControllerBase
{
    private readonly AdoNetDb _db;

    public ContractsController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? keyword, [FromQuery] Guid? roomId, [FromQuery] Guid? tenantId, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var items = new List<ContractListDto>();

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Contracts_GetAll");
        command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = SqlParameterValue.FromString(keyword);
        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(roomId);
        command.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(tenantId);
        command.Parameters.Add("@Status", SqlDbType.Int).Value = SqlParameterValue.FromNullable(status);
        command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        var totalItemsParameter = command.Parameters.Add("@TotalItems", SqlDbType.Int);
        totalItemsParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(MapContractList(reader));
        }

        await reader.CloseAsync();

        return Ok(ApiResponse<object>.SuccessResult(new PaginationResult<ContractListDto>
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
        ContractDetailDto? contract = null;

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Contracts_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            contract = new ContractDetailDto
            {
                Id = reader.GetGuidValue("Id"),
                ContractCode = reader.GetNullableStringValue("ContractCode"),
                StartDate = reader.GetDateOnlyValue("StartDate"),
                EndDate = reader.GetDateOnlyValue("EndDate"),
                RentPrice = reader.GetDecimalValue("RentPrice"),
                DepositAmount = reader.GetDecimalValue("DepositAmount"),
                Status = reader.GetIntValue("Status"),
                TotalInvoiceAmount = reader.GetDecimalValue("TotalInvoiceAmount"),
                TotalPaidAmount = reader.GetDecimalValue("TotalPaidAmount"),
                TotalDebtAmount = reader.GetDecimalValue("TotalDebtAmount")
            };
        }

        if (contract == null)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay hop dong"));
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            contract.Tenant = new TenantListDto
            {
                Id = reader.GetGuidValue("Id"),
                FullName = reader.GetStringValue("FullName"),
                PhoneNumber = reader.GetStringValue("PhoneNumber"),
                Email = reader.GetNullableStringValue("Email"),
                IdentityNumber = reader.GetStringValue("IdentityNumber"),
                Address = reader.GetNullableStringValue("Address")
            };
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            contract.Room = new RoomListDto
            {
                Id = reader.GetGuidValue("Id"),
                RoomNumber = reader.GetStringValue("RoomNumber"),
                BuildingId = reader.GetGuidValue("BuildingId"),
                BuildingName = reader.GetStringValue("BuildingName"),
                Area = reader.GetNullableDecimalValue("Area"),
                RentPrice = reader.GetDecimalValue("RentPrice"),
                Status = reader.GetIntValue("Status")
            };
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                contract.Invoices.Add(new InvoiceListDto
                {
                    Id = reader.GetGuidValue("Id"),
                    InvoiceCode = reader.GetNullableStringValue("InvoiceCode"),
                    BillingMonth = reader.GetIntValue("BillingMonth"),
                    BillingYear = reader.GetIntValue("BillingYear"),
                    TotalAmount = reader.GetDecimalValue("TotalAmount"),
                    PaidAmount = reader.GetDecimalValue("PaidAmount"),
                    Status = reader.GetIntValue("Status")
                });
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                contract.Payments.Add(new PaymentDto
                {
                    Id = reader.GetGuidValue("Id"),
                    InvoiceId = reader.GetGuidValue("InvoiceId"),
                    InvoiceCode = reader.GetNullableStringValue("InvoiceCode"),
                    Amount = reader.GetDecimalValue("Amount"),
                    PaymentDate = reader.GetNullableDateTimeValue("PaymentDate"),
                    Method = reader.GetNullableStringValue("Method") ?? string.Empty,
                    Note = reader.GetNullableStringValue("Note")
                });
            }
        }

        return Ok(ApiResponse<object>.SuccessResult(contract));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateContractDto dto)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Contracts_Create");
            AddCreateContractParameters(command, dto);
            var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
            idParameter.Direction = ParameterDirection.Output;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            var id = (Guid)idParameter.Value;
            return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.SuccessResult(id, "Tao hop dong thanh cong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateContractDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Contracts_Update");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = dto.StartDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = dto.EndDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@RentPrice", SqlDbType.Decimal).Value = dto.RentPrice;
        command.Parameters.Add("@DepositAmount", SqlDbType.Decimal).Value = dto.DepositAmount;
        command.Parameters.Add("@Status", SqlDbType.Int).Value = dto.Status;
        command.Parameters.Add("@Note", SqlDbType.NVarChar, -1).Value = SqlParameterValue.FromString(dto.Note);

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay hop dong"));
        }

        return Ok(ApiResponse.SuccessResult("Cap nhat hop dong thanh cong"));
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        return await ExecuteContractStatusProcedure(id, "sp_Contracts_Cancel", "Da huy hop dong");
    }

    [HttpPatch("{id}/finish")]
    public async Task<IActionResult> Finish(Guid id)
    {
        return await ExecuteContractStatusProcedure(id, "sp_Contracts_Finish", "Da ket thuc hop dong");
    }

    private async Task<IActionResult> ExecuteContractStatusProcedure(Guid id, string procedureName, string successMessage)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, procedureName);
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay hop dong"));
        }

        return Ok(ApiResponse.SuccessResult(successMessage));
    }

    private static void AddCreateContractParameters(SqlCommand command, CreateContractDto dto)
    {
        command.Parameters.Add("@TenantId", SqlDbType.UniqueIdentifier).Value = dto.TenantId;
        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier).Value = dto.RoomId;
        command.Parameters.Add("@StartDate", SqlDbType.Date).Value = dto.StartDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@EndDate", SqlDbType.Date).Value = dto.EndDate.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@RentPrice", SqlDbType.Decimal).Value = dto.RentPrice;
        command.Parameters.Add("@DepositAmount", SqlDbType.Decimal).Value = dto.DepositAmount;
        command.Parameters.Add("@Note", SqlDbType.NVarChar, -1).Value = SqlParameterValue.FromString(dto.Note);
    }

    private static ContractListDto MapContractList(SqlDataReader reader)
    {
        return new ContractListDto
        {
            Id = reader.GetGuidValue("Id"),
            ContractCode = reader.GetNullableStringValue("ContractCode"),
            TenantName = reader.GetStringValue("TenantName"),
            RoomNumber = reader.GetStringValue("RoomNumber"),
            BuildingName = reader.GetStringValue("BuildingName"),
            StartDate = reader.GetDateOnlyValue("StartDate"),
            EndDate = reader.GetDateOnlyValue("EndDate"),
            RentPrice = reader.GetDecimalValue("RentPrice"),
            DepositAmount = reader.GetDecimalValue("DepositAmount"),
            Status = reader.GetIntValue("Status")
        };
    }
}
