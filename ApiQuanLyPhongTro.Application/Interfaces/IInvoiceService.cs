using System;
using System.Threading.Tasks;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Application.Common;

namespace ApiQuanLyPhongTro.Application.Interfaces;

public interface IInvoiceService
{
    Task<ApiResponse<Guid>> CreateInvoiceAsync(CreateInvoiceDto dto);
    Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> RecalculateInvoiceAsync(Guid invoiceId);
    Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> UpdateInvoiceStatusAsync(Guid invoiceId);
    Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto dto);
}
