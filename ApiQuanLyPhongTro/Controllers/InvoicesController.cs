using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Application.DTO.ApiQuanLyPhongTro.DTOs;
using ApiQuanLyPhongTro.Entities;
using ApiQuanLyPhongTro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiQuanLyPhongTro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly AppDbContext _context; // Đảm bảo bạn gọi đúng tên DbContext của bạn

        public InvoiceController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // API 1: LẤY THỐNG KÊ CÔNG NỢ VÀ CHU KỲ CỦA MỘT HỢP ĐỒNG
        // Route: GET /api/Invoice/Contract/{contractId}/Summary
        // =======================================================
        [HttpGet("Contract/{contractId}/Summary")]
        public async Task<IActionResult> GetRentSummary(Guid contractId)
        {
            try
            {
                var contract = await _context.Contracts
                    .Include(c => c.Tenant)
                    .Include(c => c.Room)
                    .FirstOrDefaultAsync(c => c.Id == contractId);

                if (contract == null) return NotFound(new { message = "Không tìm thấy hợp đồng." });

                // Lấy Invoices kèm theo các dòng Details (Điện, Nước, Rác...)
                var invoices = await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.InvoiceDetails)
                    .Where(i => i.ContractId == contractId)
                    .ToListAsync();

                var summary = new RentSummaryDto
                {
                    ContractId = contract.Id,
                    TenantName = contract.Tenant?.FullName ?? "N/A",
                    RoomNumber = contract.Room?.RoomNumber ?? "N/A",
                    AgreedPrice = contract.AgreedPrice,
                    Cycles = new List<RentCycleDto>()
                };

                DateOnly currentCycleStart = contract.StartDate;
                DateOnly today = DateOnly.FromDateTime(DateTime.Today);
                int cycleCount = 1;

                while (currentCycleStart <= today && currentCycleStart < contract.EndDate)
                {
                    var invoiceForThisCycle = invoices.FirstOrDefault(i =>
                        i.BillingMonth == currentCycleStart.Month &&
                        i.BillingYear == currentCycleStart.Year);

                    bool isPaid = invoiceForThisCycle != null && invoiceForThisCycle.Status == 1;

                    // --- LOGIC TÍNH TỔNG CHI PHÍ TỪ DETAIL ---
                    decimal actualAmount;
                    if (invoiceForThisCycle != null)
                    {
                        // Tổng chi phí = Sum(Amount) của tất cả các dòng trong InvoiceDetail cho hóa đơn này
                        var detailSum = invoiceForThisCycle.InvoiceDetails.Sum(d => d.Amount);
                        // Nếu bảng Detail có dữ liệu thì lấy tổng Detail, nếu không có (hóa đơn trống) thì lấy TotalAmount
                        actualAmount = detailSum > 0 ? detailSum : invoiceForThisCycle.TotalAmount;
                    }
                    else
                    {
                        // Chưa lập hóa đơn thì tạm tính bằng tiền phòng trong hợp đồng
                        actualAmount = contract.AgreedPrice;
                    }

                    summary.Cycles.Add(new RentCycleDto
                    {
                        CycleNumber = cycleCount,
                        CycleStart = currentCycleStart.ToDateTime(TimeOnly.MinValue),
                        CycleEnd = currentCycleStart.AddMonths(1).ToDateTime(TimeOnly.MinValue),
                        ExpectedAmount = actualAmount,
                        IsPaid = isPaid,
                        InvoiceId = invoiceForThisCycle?.Id
                    });

                    summary.TotalMonthsElapsed++;
                    if (isPaid) summary.PaidMonths++;
                    else
                    {
                        summary.UnpaidMonths++;
                        summary.TotalDebt += actualAmount; // Cộng dồn nợ dựa trên chi phí thực tế
                    }

                    currentCycleStart = currentCycleStart.AddMonths(1);
                    cycleCount++;
                }
                return Ok(summary);
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }
        // =======================================================
        // CÁC API KHÁC CỦA BẠN (Tạo hóa đơn, Xóa hóa đơn...) 
        // BẠN CÓ THỂ VIẾT TIẾP Ở DƯỚI NÀY
        // =======================================================

        /* Ví dụ: 
        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice) { ... }
        */
        // =======================================================
        // API 2: LẬP HÓA ĐƠN MỚI
        // Route: POST /api/Invoice
        // =======================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Invoice invoice)
        {
            if (invoice == null) return BadRequest("Dữ liệu không hợp lệ");

            // Xóa bỏ các đối tượng liên quan nếu JS lỡ gửi kèm để tránh lỗi ràng buộc
            invoice.Contract = null!;

            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();
            return Ok("Lập hóa đơn thành công!");
        }
        // API: Cập nhật trạng thái thanh toán
        // Route: PUT /api/Invoice/{id}/Pay
        [HttpPut("{id}/Pay")]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound("Không tìm thấy hóa đơn.");

            invoice.Status = 1; // Đã thanh toán
            invoice.PaidAmount = invoice.TotalAmount; // Cập nhật số tiền đã đóng
            invoice.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Xác nhận thanh toán thành công!" });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var inv = await _context.Invoices
                .Include(i => i.InvoiceDetails) // Lấy dữ liệu từ bảng InvoiceDetail
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();

            if (inv == null) return NotFound(new { message = "Không tìm thấy hóa đơn" });

            // Tính tổng tiền thực tế từ bảng InvoiceDetail thay vì chỉ lấy tiền phòng
            var totalFromDetails = inv.InvoiceDetails.Sum(d => d.Amount);

            return Ok(new
            {
                Id = inv.Id,
                ContractId = inv.ContractId,
                BillingMonth = inv.BillingMonth,
                BillingYear = inv.BillingYear,
                Status = inv.Status,
                CreatedAt = inv.CreatedAt,
                // Nếu bảng chi tiết có dữ liệu thì lấy tổng chi tiết, nếu không lấy tiền phòng gốc
                TotalAmount = totalFromDetails > 0 ? totalFromDetails : inv.TotalAmount,
                Details = inv.InvoiceDetails.Select(d => new {
                    d.Description,
                    d.Quantity,
                    d.UnitPrice,
                    d.Amount
                }).ToList()
            });
        }
    }
}