using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyPhongTro.Models.Entities;

public class Room
{
    public Guid Id { get; set; }

    [Required]
    public Guid BuildingId { get; set; }

    [Required(ErrorMessage = "Số phòng không được để trống")]
    [MaxLength(50)]
    public string RoomNumber { get; set; } = null!;

    public int? Capacity { get; set; }

    public decimal? Area { get; set; }

    [Required(ErrorMessage = "Giá thuê không được để trống")]
    public decimal BasePrice { get; set; }

    public int? Status { get; set; } // 0=Empty, 1=Rented, 2=Maintenance

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public virtual Building? Building { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
