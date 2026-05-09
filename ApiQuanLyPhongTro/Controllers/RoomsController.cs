using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiQuanLyPhongTro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        // Nhờ hệ thống mang cái Thợ xây đa năng dành riêng cho Bảng Room tới đây
        private readonly IGenericRepository<Room> _roomRepo;

        public RoomsController(IGenericRepository<Room> roomRepo)
        {
            _roomRepo = roomRepo;
        }

        // Lấy tất cả danh sách phòng
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var rooms = await _roomRepo.GetAllAsync();
            return Ok(rooms);
        }

        // Tạo phòng mới
        [HttpPost]
        public async Task<IActionResult> Create(Room room)
        {
            await _roomRepo.AddAsync(room);
            return Ok("Tạo phòng mới thành công!");
        }
    
    [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var room = await _roomRepo.GetByIdAsync(id);

            if (room == null)
            {
                return NotFound("Không tìm thấy phòng này!");
            }

            return Ok(room);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Gọi hàm xóa từ Repo và đợi nó xong
            await _roomRepo.DeleteAsync(id);
            return Ok(new { message = "Xóa phòng thành công!" });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Room room)
        {
            if (id != room.Id) return BadRequest();

            // Cập nhật bằng Repository
            await _roomRepo.UpdateAsync(room);
            return Ok();
        }
    }
}