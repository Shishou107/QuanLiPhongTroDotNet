using ApiQuanLyPhongTro.Entities; // Thay đổi theo Namespace thực tế của bạn
using ApiQuanLyPhongTro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiQuanLyPhongTro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ControllerBase
    {
        private readonly AppDbContext _context; // Thay YourDbContext bằng tên DbContext của bạn

        public ServicesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Services
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Service>>> GetServices()
        {
            // Trả về danh sách dịch vụ sắp xếp theo tên
            return await _context.Services.OrderBy(s => s.Name).ToListAsync();
        }

        // GET: api/Services/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Service>> GetService(Guid id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                return NotFound(new { message = "Không tìm thấy dịch vụ!" });
            }

            return service;
        }

        // POST: api/Services
        [HttpPost]
        public async Task<ActionResult<Service>> CreateService(Service service)
        {
            if (service.Id == Guid.Empty)
            {
                service.Id = Guid.NewGuid();
            }

            service.CreatedAt = DateTime.Now;

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
        }

        // PUT: api/Services/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateService(Guid id, Service service)
        {
            if (id != service.Id)
            {
                return BadRequest(new { message = "ID không khớp!" });
            }

            _context.Entry(service).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Cập nhật dịch vụ thành công!" });
        }

        // DELETE: api/Services/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null)
            {
                return NotFound();
            }

            // Kiểm tra xem dịch vụ có đang được dùng trong hóa đơn nào không trước khi xóa
            var isBeingUsed = await _context.InvoiceDetails.AnyAsync(d => d.ServiceId == id);
            if (isBeingUsed)
            {
                return BadRequest(new { message = "Không thể xóa dịch vụ này vì đã có hóa đơn sử dụng!" });
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa dịch vụ thành công!" });
        }

        private bool ServiceExists(Guid id)
        {
            return _context.Services.Any(e => e.Id == id);
        }
    }
}