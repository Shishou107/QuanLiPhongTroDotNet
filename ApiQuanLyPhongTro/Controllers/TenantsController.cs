using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TenantsController : ControllerBase
{
    private readonly AdoNetDb _db;

    public TenantsController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? keyword, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var items = new List<TenantListDto>();

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Tenants_GetAll");
        command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = SqlParameterValue.FromString(keyword);
        command.Parameters.Add("@Status", SqlDbType.Int).Value = SqlParameterValue.FromNullable(status);
        command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        var totalItemsParameter = command.Parameters.Add("@TotalItems", SqlDbType.Int);
        totalItemsParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(new TenantListDto
            {
                Id = reader.GetGuidValue("Id"),
                FullName = reader.GetStringValue("FullName"),
                PhoneNumber = reader.GetStringValue("PhoneNumber"),
                Email = reader.GetNullableStringValue("Email"),
                IdentityNumber = reader.GetStringValue("IdentityNumber"),
                Address = reader.GetNullableStringValue("Address"),
                CurrentRoom = reader.GetNullableStringValue("CurrentRoom"),
                CurrentBuilding = reader.GetNullableStringValue("CurrentBuilding"),
                StatusText = reader.GetStringValue("StatusText")
            });
        }

        await reader.CloseAsync();

        var result = new PaginationResult<TenantListDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = (int)totalItemsParameter.Value
        };

        return Ok(ApiResponse<object>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        TenantDetailDto? tenant = null;

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Tenants_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            tenant = new TenantDetailDto
            {
                Id = reader.GetGuidValue("Id"),
                FullName = reader.GetStringValue("FullName"),
                PhoneNumber = reader.GetStringValue("PhoneNumber"),
                Email = reader.GetNullableStringValue("Email"),
                IdentityNumber = reader.GetStringValue("IdentityNumber"),
                DateOfBirth = reader.GetNullableDateOnlyValue("DateOfBirth"),
                Address = reader.GetNullableStringValue("Address"),
                TotalPaidAmount = reader.GetDecimalValue("TotalPaidAmount"),
                TotalDebtAmount = reader.GetDecimalValue("TotalDebtAmount")
            };
        }

        if (tenant == null)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay khach thue"));
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            tenant.CurrentContract = MapContractList(reader);
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                tenant.Contracts.Add(MapContractList(reader));
            }
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                tenant.Invoices.Add(new InvoiceListDto
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

        return Ok(ApiResponse<object>.SuccessResult(tenant));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTenantDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Tenants_Create");
        AddTenantParameters(command, dto);
        var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
        idParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();

        var id = (Guid)idParameter.Value;
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.SuccessResult(id, "Tao khach thue thanh cong"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateTenantDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Tenants_Update");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        AddTenantParameters(command, dto);

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay khach thue"));
        }

        return Ok(ApiResponse.SuccessResult("Cap nhat khach thue thanh cong"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Tenants_Delete");
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay khach thue"));
            }

            return Ok(ApiResponse.SuccessResult("Da xoa khach thue"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    private static void AddTenantParameters(SqlCommand command, CreateTenantDto dto)
    {
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = dto.FullName;
        command.Parameters.Add("@PhoneNumber", SqlDbType.VarChar, 15).Value = dto.PhoneNumber;
        command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = SqlParameterValue.FromString(dto.Email);
        command.Parameters.Add("@IdentityNumber", SqlDbType.VarChar, 20).Value = dto.IdentityNumber;
        command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = dto.DateOfBirth.HasValue ? dto.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        command.Parameters.Add("@Address", SqlDbType.NVarChar, 255).Value = SqlParameterValue.FromString(dto.Address);
    }

    private static ContractListDto MapContractList(SqlDataReader reader)
    {
        return new ContractListDto
        {
            Id = reader.GetGuidValue("Id"),
            ContractCode = reader.GetNullableStringValue("ContractCode"),
            TenantName = reader.GetNullableStringValue("TenantName") ?? string.Empty,
            RoomNumber = reader.GetNullableStringValue("RoomNumber") ?? string.Empty,
            BuildingName = reader.GetNullableStringValue("BuildingName") ?? string.Empty,
            StartDate = reader.GetDateOnlyValue("StartDate"),
            EndDate = reader.GetDateOnlyValue("EndDate"),
            RentPrice = reader.GetDecimalValue("RentPrice"),
            DepositAmount = reader.GetDecimalValue("DepositAmount"),
            Status = reader.GetIntValue("Status")
        };
    }
}
