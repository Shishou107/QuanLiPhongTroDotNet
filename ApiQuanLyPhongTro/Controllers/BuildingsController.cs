using System.Data;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BuildingsController : ControllerBase
{
    private readonly AdoNetDb _db;

    public BuildingsController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Buildings_GetAll");
        var table = _db.FillDataTable(command);

        var buildings = table.Rows
            .Cast<DataRow>()
            .Select(MapBuildingList)
            .ToList();

        return Ok(ApiResponse<object>.SuccessResult(buildings));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        using var connection = _db.CreateConnection();
        using var command = _db.CreateStoredProcedureCommand(connection, "sp_Buildings_GetById");
        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

        var dataSet = _db.FillDataSet(command);
        var buildingTable = dataSet.Tables.Count > 0 ? dataSet.Tables[0] : new DataTable();

        if (buildingTable.Rows.Count == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay toa nha"));
        }

        var row = buildingTable.Rows[0];
        var building = new BuildingDetailDto
        {
            Id = row.GetGuidValue("Id"),
            Name = row.GetStringValue("Name"),
            Address = row.GetNullableStringValue("Address"),
            Description = row.GetNullableStringValue("Description")
        };

        if (dataSet.Tables.Count > 1)
        {
            foreach (DataRow roomRow in dataSet.Tables[1].Rows)
            {
                building.Rooms.Add(new RoomListDto
                {
                    Id = roomRow.GetGuidValue("Id"),
                    RoomNumber = roomRow.GetStringValue("RoomNumber"),
                    BuildingId = roomRow.GetGuidValue("BuildingId"),
                    BuildingName = roomRow.GetStringValue("BuildingName"),
                    Area = roomRow.GetNullableDecimalValue("Area"),
                    RentPrice = roomRow.GetDecimalValue("RentPrice"),
                    Status = roomRow.GetIntValue("Status")
                });
            }
        }

        return Ok(ApiResponse<object>.SuccessResult(building));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBuildingDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Buildings_Create");

        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = dto.Name;
        command.Parameters.Add("@Address", SqlDbType.NVarChar, 255).Value = SqlParameterValue.FromString(dto.Address);
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = SqlParameterValue.FromString(dto.Description);

        var idParameter = command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier);
        idParameter.Direction = ParameterDirection.Output;

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();

        var id = (Guid)idParameter.Value;
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.SuccessResult(id, "Tao toa nha thanh cong"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, CreateBuildingDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Buildings_Update");

        command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = dto.Name;
        command.Parameters.Add("@Address", SqlDbType.NVarChar, 255).Value = SqlParameterValue.FromString(dto.Address);
        command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = SqlParameterValue.FromString(dto.Description);

        await connection.OpenAsync();
        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            return NotFound(ApiResponse.FailureResult("Khong tim thay toa nha"));
        }

        return Ok(ApiResponse.SuccessResult("Cap nhat toa nha thanh cong"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await using var connection = _db.CreateConnection();
            await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Buildings_Delete");

            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;

            await connection.OpenAsync();
            var rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                return NotFound(ApiResponse.FailureResult("Khong tim thay toa nha"));
            }

            return Ok(ApiResponse.SuccessResult("Da xoa toa nha"));
        }
        catch (SqlException ex)
        {
            return BadRequest(ApiResponse.FailureResult(ex.Message));
        }
    }

    private static BuildingListDto MapBuildingList(DataRow row)
    {
        return new BuildingListDto
        {
            Id = row.GetGuidValue("Id"),
            Name = row.GetStringValue("Name"),
            Address = row.GetNullableStringValue("Address"),
            Description = row.GetNullableStringValue("Description"),
            TotalRooms = row.GetIntValue("TotalRooms"),
            EmptyRooms = row.GetIntValue("EmptyRooms"),
            RentedRooms = row.GetIntValue("RentedRooms"),
            MaintenanceRooms = row.GetIntValue("MaintenanceRooms")
        };
    }
}
