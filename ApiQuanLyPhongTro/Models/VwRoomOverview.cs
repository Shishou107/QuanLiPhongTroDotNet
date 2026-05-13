using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Models;

public partial class VwRoomOverview
{
    public string BuildingName { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public decimal? Area { get; set; }

    public decimal BasePrice { get; set; }

    public string? RoomStatus { get; set; }

    public string? CurrentTenant { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? AgreedPrice { get; set; }
}
