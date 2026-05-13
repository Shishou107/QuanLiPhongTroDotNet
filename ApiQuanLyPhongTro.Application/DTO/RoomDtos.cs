using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Application.DTO;

public class RoomListDto
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public decimal? Area { get; set; }
    public decimal RentPrice { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Trống",
        1 => "Đang thuê",
        2 => "Bảo trì",
        _ => "Không xác định"
    };
    public string? CurrentTenantName { get; set; }
    public Guid? CurrentContractId { get; set; }
}

public class RoomDetailDto
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public decimal? Area { get; set; }
    public decimal RentPrice { get; set; }
    public int Status { get; set; }
    public string StatusText => Status switch
    {
        0 => "Trống",
        1 => "Đang thuê",
        2 => "Bảo trì",
        _ => "Không xác định"
    };
    public string? Description { get; set; }
    public ContractListDto? CurrentContract { get; set; }
    public List<InvoiceListDto> RecentInvoices { get; set; } = new();
    public decimal BasePrice { get; set; }
}


public class CreateRoomDto
{
    public Guid BuildingId { get; set; }
    public string RoomNumber { get; set; } = null!;
    public int? Capacity { get; set; }
    public decimal? Area { get; set; }
    public decimal BasePrice { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
}

public class UpdateRoomStatusDto
{
    public int Status { get; set; }
}
