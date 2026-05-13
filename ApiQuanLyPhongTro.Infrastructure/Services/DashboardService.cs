using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiQuanLyPhongTro.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var totalBuildings = await _context.Buildings.CountAsync();
        var totalRooms = await _context.Rooms.CountAsync();
        var emptyRooms = await _context.Rooms.CountAsync(r => r.Status == 0);
        var rentedRooms = await _context.Rooms.CountAsync(r => r.Status == 1);
        var maintenanceRooms = await _context.Rooms.CountAsync(r => r.Status == 2);
        var totalTenants = await _context.Tenants.CountAsync();
        var activeContracts = await _context.Contracts.CountAsync(c => c.Status == 1);
        var unpaidInvoices = await _context.Invoices.CountAsync(i => i.Status == 0 || i.Status == 1);
        var overdueInvoices = await _context.Invoices.CountAsync(i => i.Status == 3);
        
        var totalPaidAmount = await _context.Invoices.SumAsync(i => i.PaidAmount) ?? 0;

        var totalAmount = await _context.Invoices.SumAsync(i => i.TotalAmount);
        var totalDebtAmount = totalAmount - totalPaidAmount;

        return new DashboardSummaryDto
        {
            TotalBuildings = totalBuildings,
            TotalRooms = totalRooms,
            EmptyRooms = emptyRooms,
            RentedRooms = rentedRooms,
            MaintenanceRooms = maintenanceRooms,
            TotalTenants = totalTenants,
            ActiveContracts = activeContracts,
            UnpaidInvoices = unpaidInvoices,
            OverdueInvoices = overdueInvoices,
            TotalPaidAmount = totalPaidAmount,
            TotalDebtAmount = totalDebtAmount
        };
    }

    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(int year)
    {
        var revenues = await _context.Invoices
            .Where(i => i.BillingYear == year)
            .GroupBy(i => i.BillingMonth)
            .Select(g => new MonthlyRevenueDto
            {
                Month = g.Key,
                TotalInvoiceAmount = g.Sum(i => i.TotalAmount),
                TotalPaidAmount = g.Sum(i => i.PaidAmount) ?? 0,
                TotalDebtAmount = g.Sum(i => i.TotalAmount) - (g.Sum(i => i.PaidAmount) ?? 0)


            })
            .OrderBy(r => r.Month)
            .ToListAsync();

        return revenues;
    }

    public async Task<List<RoomStatusStatsDto>> GetRoomStatusStatisticsAsync()
    {
        return await _context.Rooms
            .GroupBy(r => r.Status)
            .Select(g => new RoomStatusStatsDto
            {
                Status = g.Key ?? 0,
                StatusText = g.Key == 0 ? "Trống" : (g.Key == 1 ? "Đang thuê" : "Bảo trì"),
                Count = g.Count()
            })
            .ToListAsync();
    }
}
