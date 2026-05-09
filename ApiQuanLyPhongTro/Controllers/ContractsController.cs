using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Entities;
using ApiQuanLyPhongTro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiQuanLyPhongTro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IGenericRepository<Contract> _contractRepo;
        private readonly AppDbContext _AppDbContext;

        public ContractsController(IGenericRepository<Contract> contractRepo, AppDbContext appDbContext )
        {
            _contractRepo = contractRepo;
            _AppDbContext = appDbContext;
        }

        [HttpGet]
        // Trong GenericRepository.cs hoặc ContractRepository.cs
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var contracts = await _AppDbContext.Contracts
                    .Include(c => c.Room)   // Vẫn cần Include để lấy data
                    .Include(c => c.Tenant)
                    .Select(c => new
                    {
                        // Chỉ lấy những trường phẳng, không lôi nguyên Object
                        Id = c.Id,
                        ContractNumber = c.Id,
                        TenantName = c.Tenant != null ? c.Tenant.FullName : "N/A",
                        RoomNumber = c.Room != null ? c.Room.RoomNumber : "N/A",
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        DepositAmount = c.DepositAmount,
                        Status = c.Status
                    })
                    .ToListAsync();

                return Ok(contracts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi xử lý dữ liệu", detail = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var contract = await _contractRepo.GetByIdAsync(id);
            if (contract == null) return NotFound();
            return Ok(contract);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Contract contract)
        {
            // 1. Lưu hợp đồng mới
            await _contractRepo.AddAsync(contract);

            // 2. Cập nhật trạng thái phòng thành 1 (Đã thuê)
            var room = await _AppDbContext.Rooms.FindAsync(contract.RoomId);
            if (room != null)
            {
                room.Status = 1; // 1 = Đã thuê
                _AppDbContext.Rooms.Update(room);
                await _AppDbContext.SaveChangesAsync();
            }

            return Ok(new { message = "Tạo hợp đồng thành công!" });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Contract contract)
        {
            if (id != contract.Id) return BadRequest();
            await _contractRepo.UpdateAsync(contract);
            return Ok("Cập nhật hợp đồng thành công!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // 1. Tìm hợp đồng trước để lấy mã Phòng (RoomId)
            var contract = await _AppDbContext.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound("Không tìm thấy hợp đồng này.");
            }

            var roomId = contract.RoomId;

            // 2. Tiến hành xóa hợp đồng
            await _contractRepo.DeleteAsync(id);

            // 3. Cập nhật trạng thái phòng thành 0 hoặc 2
            var room = await _AppDbContext.Rooms.FindAsync(roomId);
            if (room != null)
            {
                room.Status = 2; // Bạn có thể để 0 (Trống) hoặc 2 (Chờ dọn) tùy ý
                _AppDbContext.Rooms.Update(room);
                await _AppDbContext.SaveChangesAsync();
            }

            return Ok(new { message = "Xóa hợp đồng và trả phòng thành công!" });
        }
    }
}
