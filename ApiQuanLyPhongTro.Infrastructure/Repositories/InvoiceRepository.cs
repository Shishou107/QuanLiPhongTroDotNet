using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Entities;
using ApiQuanLyPhongTro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiQuanLyPhongTro.Infrastructure.Repositories
{
    public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync()
        {
            return await _context.Invoices
                .Where(i => i.Status == 0 || i.Status == 1) 
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueByMonthAsync(int month, int year)
        {
            return await _context.Invoices
                .Where(i => i.BillingMonth == month && i.BillingYear == year && i.Status == 2)
                .SumAsync(i => i.TotalAmount);
        }
    }
}
