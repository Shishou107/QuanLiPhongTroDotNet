using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class PaymentService
{
    private readonly BaseApiService _apiService;

    public PaymentService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginationResult<PaymentListViewModel>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        var response = await _apiService.GetAsync<PaginationResult<PaymentListViewModel>>($"payments?page={page}&pageSize={pageSize}");
        return response?.Data ?? new PaginationResult<PaymentListViewModel>();
    }

    public async Task<PaymentListViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<PaymentListViewModel>($"payments/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(PaymentCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("payments", model);
    }
}
