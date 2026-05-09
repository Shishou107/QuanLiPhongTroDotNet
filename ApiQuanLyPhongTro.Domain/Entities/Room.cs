using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Entities;

public partial class Room
{
    public Guid Id { get; set; }

    public Guid BuildingId { get; set; }

    public string RoomNumber { get; set; } = null!;

    public int? Capacity { get; set; }

    public decimal? Area { get; set; }

    public decimal BasePrice { get; set; }

    public int? Status { get; set; }
    public string? ImageUrl { get; set; } 
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Building? Building { get; set; } = null!;

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
