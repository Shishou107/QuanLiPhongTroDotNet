using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RoomsController : ControllerBase
{
    private readonly AdoNetDb _db;

    public RoomsController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? buildingId, [FromQuery] int? status, [FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var items = new List<RoomListDto>();

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_GetAll");
        command.Parameters.Add("@BuildingId", SqlDbType.UniqueIdentifier).Value = SqlParameterValue.FromNullable(buildingId);
        command.Parameters.Add("@Status", SqlDbType.Int).Value = SqlParameterValue.FromNullable(status);
        command.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = SqlParameterValue.FromString(keyword);
        command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
        command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
        var totalItemsParameter = command.Parameters.Add("@TotalItems", SqlDbType.Int);
        totalItemsParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            items.Add(MapRoomList(reader));
        }

        await reader.CloseAsync();

        var result = new PaginationResult<RoomListDto>
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
        RoomDetailDto? room = null;

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            room = new RoomDetailDto
            {
                Id = reader.GetGuidValue("Id"),
                RoomNumber = reader.GetStringValue("RoomNumber"),
                BuildingId = reader.GetGuidValue("BuildingId"),
                BuildingName = reader.GetStringValue("BuildingName"),
                Area = reader.GetNullableDecimalValue("Area"),
                RentPrice = reader.GetDecimalValue("RentPrice"),
                BasePrice = reader.GetDecimalValue("BasePrice"),
                Status = reader.GetIntValue("Status"),
                Description = reader.GetNullableStringValue("Description")
            };
        }

        if (room == null)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay phong"));
        }

        if (await reader.NextResultAsync() && await reader.ReadAsync())
        {
            room.CurrentContract = new ContractListDto
            {
                Id = reader.GetGuidValue("Id"),
                ContractCode = reader.GetNullableStringValue("ContractCode"),
                TenantName = reader.GetStringValue("TenantName"),
                StartDate = reader.GetDateOnlyValue("StartDate"),
                EndDate = reader.GetDateOnlyValue("EndDate"),
                Status = reader.GetIntValue("Status")
            };
        }

        if (await reader.NextResultAsync())
        {
            while (await reader.ReadAsync())
            {
                room.RecentInvoices.Add(new InvoiceListDto
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

        return Ok(ApiResponse<object>.SuccessResult(room));
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var rooms = new List<RoomListDto>();

        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_GetAvailable");

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rooms.Add(MapRoomList(reader));
        }

        return Ok(ApiResponse<object>.SuccessResult(rooms));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_Create");
        AddRoomParameters(command, dto);
        var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
        idParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();

        var id = (Guid)idParameter.Value;
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.SuccessResult(id, "Tao phong thanh cong"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateRoomDto dto)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            if (dto.Status == 0 && await HasActiveContractAsync(connection, id))
            {
                return BadRequest(ApiResponse.FailureResult("Phong dang co hop dong hieu luc, khong the chuyen sang Trong. Hay ket thuc hoac huy hop dong truoc."));
            }

            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_Update");
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
            AddRoomParameters(command, dto);

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay phong"));
            }

            return Ok(ApiResponse.SuccessResult("Cap nhat phong thanh cong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateRoomStatusDto dto)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await connection.OpenAsync();

            if (dto.Status == 0 && await HasActiveContractAsync(connection, id))
            {
                return BadRequest(ApiResponse.FailureResult("Phong dang co hop dong hieu luc, khong the chuyen sang Trong. Hay ket thuc hoac huy hop dong truoc."));
            }

            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_UpdateStatus");
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
            command.Parameters.Add("@Status", SqlDbType.Int).Value = dto.Status;

            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay phong"));
            }

            return Ok(ApiResponse.SuccessResult("Cap nhat trang thai thanh cong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Rooms_Delete");
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay phong"));
            }

            return Ok(ApiResponse.SuccessResult("Da xoa phong"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    private static RoomListDto MapRoomList(SqlDataReader reader)
    {
        return new RoomListDto
        {
            Id = reader.GetGuidValue("Id"),
            RoomNumber = reader.GetStringValue("RoomNumber"),
            BuildingId = reader.GetGuidValue("BuildingId"),
            BuildingName = reader.GetStringValue("BuildingName"),
            Area = reader.GetNullableDecimalValue("Area"),
            RentPrice = reader.GetDecimalValue("RentPrice"),
            Status = reader.GetIntValue("Status"),
            CurrentTenantName = reader.GetNullableStringValue("CurrentTenantName"),
            CurrentContractId = reader.GetNullableGuidValue("CurrentContractId")
        };
    }

    private static void AddRoomParameters(SqlCommand command, CreateRoomDto dto)
    {
        command.Parameters.Add("@BuildingId", SqlDbType.UniqueIdentifier).Value = dto.BuildingId;
        command.Parameters.Add("@RoomNumber", SqlDbType.NVarChar, 50).Value = dto.RoomNumber;
        command.Parameters.Add("@Capacity", SqlDbType.Int).Value = SqlParameterValue.FromNullable(dto.Capacity);
        command.Parameters.Add("@Area", SqlDbType.Decimal).Value = SqlParameterValue.FromNullable(dto.Area);
        command.Parameters.Add("@BasePrice", SqlDbType.Decimal).Value = dto.BasePrice;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = SqlParameterValue.FromString(dto.Description);
        command.Parameters.Add("@Status", SqlDbType.Int).Value = SqlParameterValue.FromNullable(dto.Status);
    }

    private static async Task<bool> HasActiveContractAsync(SqlConnection connection, Guid roomId)
    {
        await using var command = new SqlCommand(
            "SELECT COUNT(1) FROM dbo.Contracts WHERE RoomId = @RoomId AND ISNULL(Status, 0) = 1",
            connection);
        command.Parameters.Add("@RoomId", SqlDbType.UniqueIdentifier).Value = roomId;

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
}
