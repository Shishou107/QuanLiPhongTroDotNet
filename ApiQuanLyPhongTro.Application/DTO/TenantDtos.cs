using System;
using System.Collections.Generic;

namespace ApiQuanLyPhongTro.Application.DTO;

public class TenantListDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string IdentityNumber { get; set; } = null!;
    public string? Address { get; set; }
    public string? CurrentRoom { get; set; }
    public string? CurrentBuilding { get; set; }
    public string StatusText { get; set; } = string.Empty;
}

public class TenantDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string IdentityNumber { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public ContractListDto? CurrentContract { get; set; }
    public List<ContractListDto> Contracts { get; set; } = new();
    public List<InvoiceListDto> Invoices { get; set; } = new();
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalDebtAmount { get; set; }
}

public class CreateTenantDto
{
    public string FullName { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public string IdentityNumber { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
}
