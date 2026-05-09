using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiQuanLyPhongTro.Application.DTO
{
    // Đặt trong thư mục DTOs, file RentSummaryDto.cs
    using System;
    using System.Collections.Generic;

    namespace ApiQuanLyPhongTro.DTOs
    {
        public class RentSummaryDto
        {
            public Guid ContractId { get; set; }
            public string TenantName { get; set; }
            public string RoomNumber { get; set; }
            public decimal AgreedPrice { get; set; }

            public int TotalMonthsElapsed { get; set; }
            public int PaidMonths { get; set; }
            public int UnpaidMonths { get; set; }
            public decimal TotalDebt { get; set; }

            public List<RentCycleDto> Cycles { get; set; } = new List<RentCycleDto>();
        }

        public class RentCycleDto
        {
            public int CycleNumber { get; set; }
            public DateTime CycleStart { get; set; }
            public DateTime CycleEnd { get; set; }
            public decimal ExpectedAmount { get; set; }
            public bool IsPaid { get; set; }
            public Guid? InvoiceId { get; set; }
        }
    }
}
