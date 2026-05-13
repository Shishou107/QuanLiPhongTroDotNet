using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class PaymentService
{
    private readonly BaseApiService _apiService;

    public PaymentService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginationResult<PaymentListViewModel>> GetAllAsync(string? keyword = null, DateTime? fromDate = null, DateTime? toDate = null, string? method = null, int page = 1, int pageSize = 10)
    {
        var url = $"payments?keyword={keyword}&fromDate={fromDate:yyyy-MM-dd}&toDate={toDate:yyyy-MM-dd}&method={method}&page={page}&pageSize={pageSize}";
        var response = await _apiService.GetAsync<PaginationResult<PaymentListViewModel>>(url);
        return response?.Data ?? new PaginationResult<PaymentListViewModel>();
    }

    public async Task<PaymentListViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<PaymentListViewModel>($"payments/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(PaymentCreateViewModel model)
    {
        var dto = new
        {
            InvoiceId = model.InvoiceId,
            Amount = model.Amount,
            Method = model.PaymentMethod.ToString(),
            PaymentDate = model.PaymentDate,
            Note = model.Note
        };
        return await _apiService.PostAsync<Guid>("payments", dto);
    }
}
