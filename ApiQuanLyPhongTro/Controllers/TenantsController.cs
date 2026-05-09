using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiQuanLyPhongTro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TenantsController : ControllerBase
    {
        private readonly IGenericRepository<Tenant> _tenantRepo;

        public TenantsController(IGenericRepository<Tenant> tenantRepo)
        {
            _tenantRepo = tenantRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _tenantRepo.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tenant = await _tenantRepo.GetByIdAsync(id);
            if (tenant == null) return NotFound("Không tìm thấy khách thuê này.");
            return Ok(tenant);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Tenant tenant)
        {
            await _tenantRepo.AddAsync(tenant);
            return Ok(new { Message = "Thêm khách thuê thành công!", Data = tenant });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Tenant tenant)
        {
            if (id != tenant.Id) return BadRequest("ID không khớp.");
            await _tenantRepo.UpdateAsync(tenant);
            return Ok("Cập nhật thành công!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _tenantRepo.DeleteAsync(id);
            return Ok("Xóa khách thuê thành công!");
        }

    }
}