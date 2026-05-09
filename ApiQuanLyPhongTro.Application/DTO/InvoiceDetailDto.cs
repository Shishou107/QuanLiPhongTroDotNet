using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiQuanLyPhongTro.Application.DTO
{
    public class InvoiceDetailDto
    {
        public Guid Id { get; set; }
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }
        public decimal TotalAmount { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string RoomNumber { get; set; } // Lấy từ Contract.Room
        public string TenantName { get; set; } // Lấy từ Contract.Tenant
    }
}
