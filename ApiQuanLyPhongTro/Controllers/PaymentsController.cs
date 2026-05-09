using ApiQuanLyPhongTro.Application.Interfaces;
using ApiQuanLyPhongTro.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ApiQuanLyPhongTro.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IGenericRepository<Payment> _paymentRepo;

        public PaymentsController(IGenericRepository<Payment> paymentRepo)
        {
            _paymentRepo = paymentRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _paymentRepo.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Create(Payment payment)
        {
            payment.PaymentDate = DateTime.Now; 
            await _paymentRepo.AddAsync(payment);
            return Ok("Lưu lịch sử thu tiền thành công!");
        }
    }
}
