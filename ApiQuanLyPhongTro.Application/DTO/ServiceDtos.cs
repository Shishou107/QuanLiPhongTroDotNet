using System;

namespace ApiQuanLyPhongTro.Application.DTO;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal DefaultPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateServiceDto
{
    public string Name { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal DefaultPrice { get; set; }
}
