using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Infrastructure.Data;
using ApiQuanLyPhongTro.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiQuanLyPhongTro.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _context;

    public InvoiceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Guid>> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        var contract = await _context.Contracts.FindAsync(dto.ContractId);
        if (contract == null) return ApiResponse<Guid>.FailureResult("Hợp đồng không tồn tại");
        if (contract.Status != 1) return ApiResponse<Guid>.FailureResult("Hợp đồng không còn hiệu lực");

        if (dto.BillingMonth < 1 || dto.BillingMonth > 12) return ApiResponse<Guid>.FailureResult("Tháng không hợp lệ");

        var existing = await _context.Invoices.AnyAsync(i => 
            i.ContractId == dto.ContractId && 
            i.BillingMonth == dto.BillingMonth && 
            i.BillingYear == dto.BillingYear);
        
        if (existing) return ApiResponse<Guid>.FailureResult("Hóa đơn cho tháng này đã tồn tại");

        if (dto.Details == null || !dto.Details.Any()) return ApiResponse<Guid>.FailureResult("Hóa đơn phải có ít nhất 1 chi tiết");

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ContractId = dto.ContractId,
            BillingMonth = dto.BillingMonth,
            BillingYear = dto.BillingYear,
            DueDate = dto.DueDate,
            Status = 0, // Unpaid
            PaidAmount = 0,
            CreatedAt = DateTime.Now
        };

        decimal total = 0;
        foreach (var detailDto in dto.Details)
        {
            var service = await _context.Services.FindAsync(detailDto.ServiceId);
            if (service == null) return ApiResponse<Guid>.FailureResult($"Dịch vụ {detailDto.ServiceId} không tồn tại");

            var amount = detailDto.Quantity * detailDto.UnitPrice;
            total += amount;

            invoice.InvoiceDetails!.Add(new InvoiceDetail
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ServiceId = detailDto.ServiceId,
                Description = service.Name,
                Quantity = detailDto.Quantity,
                UnitPrice = detailDto.UnitPrice,
                Amount = amount,
                CreatedAt = DateTime.Now
            });
        }

        invoice.TotalAmount = total;
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return ApiResponse<Guid>.SuccessResult(invoice.Id, "Tạo hóa đơn thành công");
    }

    public async Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> RecalculateInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceDetails)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return ApiQuanLyPhongTro.Application.Common.ApiResponse.FailureResult("Không tìm thấy hóa đơn");

        invoice.TotalAmount = invoice.InvoiceDetails != null ? invoice.InvoiceDetails.Sum(d => d.Amount) : 0;
        invoice.PaidAmount = invoice.Payments != null ? invoice.Payments.Sum(p => p.Amount) : 0;


        // Update status
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (invoice.PaidAmount >= invoice.TotalAmount)
        {
            invoice.Status = 2; // Paid
        }
        else if (invoice.PaidAmount > 0)
        {
            invoice.Status = 1; // Partially Paid
        }
        else
        {
            if (invoice.DueDate.HasValue && invoice.DueDate.Value < today)
                invoice.Status = 3; // Overdue
            else
                invoice.Status = 0; // Unpaid
        }

        invoice.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return ApiQuanLyPhongTro.Application.Common.ApiResponse.SuccessResult("Đã cập nhật lại hóa đơn");
    }

    public async Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> UpdateInvoiceStatusAsync(Guid invoiceId)
    {
        return await RecalculateInvoiceAsync(invoiceId);
    }

    public async Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto dto)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceDetails)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return ApiQuanLyPhongTro.Application.Common.ApiResponse.FailureResult("Không tìm thấy hóa đơn");

        invoice.BillingMonth = dto.BillingMonth;
        invoice.BillingYear = dto.BillingYear;
        invoice.DueDate = dto.DueDate;
        invoice.Status = dto.Status;
        invoice.UpdatedAt = DateTime.Now;

        // Clear existing details and add new ones (Simplified approach)
        if (invoice.InvoiceDetails != null)
        {
            _context.InvoiceDetails.RemoveRange(invoice.InvoiceDetails);
        }

        decimal total = 0;
        foreach (var detailDto in dto.Details)
        {
            var service = await _context.Services.FindAsync(detailDto.ServiceId);
            var description = service?.Name ?? "Dịch vụ";
            
            var amount = detailDto.Quantity * detailDto.UnitPrice;
            total += amount;

            _context.InvoiceDetails.Add(new InvoiceDetail
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                ServiceId = detailDto.ServiceId,
                Description = description,
                Quantity = detailDto.Quantity,
                UnitPrice = detailDto.UnitPrice,
                Amount = amount,
                CreatedAt = DateTime.Now
            });
        }

        invoice.TotalAmount = total;
        await _context.SaveChangesAsync();
        
        // Recalculate to ensure status/paid amount is correct if needed
        await RecalculateInvoiceAsync(id);

        return ApiQuanLyPhongTro.Application.Common.ApiResponse.SuccessResult("Cập nhật hóa đơn thành công");
    }
}
