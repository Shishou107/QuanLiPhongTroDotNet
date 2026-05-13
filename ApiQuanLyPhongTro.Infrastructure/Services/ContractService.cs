using System;
using System.Linq;
using System.Threading.Tasks;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Infrastructure.Data;
using ApiQuanLyPhongTro.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiQuanLyPhongTro.Infrastructure.Services;

public class ContractService : IContractService
{
    private readonly AppDbContext _context;

    public ContractService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<Guid>> CreateContractAsync(CreateContractDto dto)
    {
        // Validation
        var room = await _context.Rooms.FindAsync(dto.RoomId);
        if (room == null) return ApiResponse<Guid>.FailureResult("Phòng không tồn tại");
        
        var tenant = await _context.Tenants.FindAsync(dto.TenantId);
        if (tenant == null) return ApiResponse<Guid>.FailureResult("Khách thuê không tồn tại");

        if (room.Status != 0) return ApiResponse<Guid>.FailureResult("Phòng không trống");

        if (dto.EndDate <= dto.StartDate) return ApiResponse<Guid>.FailureResult("Ngày kết thúc phải sau ngày bắt đầu");
        if (dto.RentPrice <= 0) return ApiResponse<Guid>.FailureResult("Giá thuê phải lớn hơn 0");

        var existingActiveContract = await _context.Contracts
            .AnyAsync(c => c.RoomId == dto.RoomId && c.Status == 1);
        if (existingActiveContract) return ApiResponse<Guid>.FailureResult("Phòng đã có hợp đồng đang hiệu lực");

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            RoomId = dto.RoomId,
            TenantId = dto.TenantId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AgreedPrice = dto.RentPrice,
            DepositAmount = dto.DepositAmount,
            Status = 1, // Active
            Note = dto.Note,
            CreatedAt = DateTime.Now
        };

        _context.Contracts.Add(contract);
        
        // Update room status
        room.Status = 1;
        room.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return ApiResponse<Guid>.SuccessResult(contract.Id, "Tạo hợp đồng thành công");
    }

    public async Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> CancelContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null) return ApiQuanLyPhongTro.Application.Common.ApiResponse.FailureResult("Không tìm thấy hợp đồng");

        contract.Status = 2; // Cancelled
        contract.UpdatedAt = DateTime.Now;

        // Check if room has other active contracts
        var hasOtherActive = await _context.Contracts
            .AnyAsync(c => c.RoomId == contract.RoomId && c.Status == 1 && c.Id != contractId);
        
        if (!hasOtherActive)
        {
            var room = await _context.Rooms.FindAsync(contract.RoomId);
            if (room != null)
            {
                room.Status = 0; // Empty
                room.UpdatedAt = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
        return ApiQuanLyPhongTro.Application.Common.ApiResponse.SuccessResult("Đã hủy hợp đồng");
    }

    public async Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> FinishContractAsync(Guid contractId)
    {
        var contract = await _context.Contracts.FindAsync(contractId);
        if (contract == null) return ApiQuanLyPhongTro.Application.Common.ApiResponse.FailureResult("Không tìm thấy hợp đồng");

        contract.Status = 0; // Finished/Expired
        contract.UpdatedAt = DateTime.Now;

        var hasOtherActive = await _context.Contracts
            .AnyAsync(c => c.RoomId == contract.RoomId && c.Status == 1 && c.Id != contractId);
        
        if (!hasOtherActive)
        {
            var room = await _context.Rooms.FindAsync(contract.RoomId);
            if (room != null)
            {
                room.Status = 0; // Empty
                room.UpdatedAt = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync();
        return ApiQuanLyPhongTro.Application.Common.ApiResponse.SuccessResult("Đã kết thúc hợp đồng");
    }

    public async Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> UpdateContractAsync(Guid id, UpdateContractDto dto)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return ApiQuanLyPhongTro.Application.Common.ApiResponse.FailureResult("Không tìm thấy hợp đồng");

        contract.StartDate = dto.StartDate;
        contract.EndDate = dto.EndDate;
        contract.AgreedPrice = dto.RentPrice;
        contract.DepositAmount = dto.DepositAmount;
        contract.Status = dto.Status;
        contract.Note = dto.Note;
        contract.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return ApiQuanLyPhongTro.Application.Common.ApiResponse.SuccessResult("Cập nhật hợp đồng thành công");
    }
}
