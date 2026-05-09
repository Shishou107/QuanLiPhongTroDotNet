using ApiQuanLyPhongTro.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiQuanLyPhongTro.Application.Interfaces
{
    public interface IInvoiceRepository : IGenericRepository<Invoice>
    {
        // Chức năng nâng cao cho Báo cáo (như trong ảnh yêu cầu)
        Task<IEnumerable<Invoice>> GetUnpaidInvoicesAsync(); // Lấy hóa đơn chưa đóng tiền
        Task<decimal> GetTotalRevenueByMonthAsync(int month, int year); // Báo cáo doanh thu tháng
    }
}
