using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Application.DTO;

public class BuildingListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public int TotalRooms { get; set; }
    public int EmptyRooms { get; set; }
    public int RentedRooms { get; set; }
    public int MaintenanceRooms { get; set; }
}

public class BuildingDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public List<RoomListDto> Rooms { get; set; } = new();
}

public class CreateBuildingDto
{
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Description { get; set; }
}
