using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class InvoiceService
{
    private readonly BaseApiService _apiService;

    public InvoiceService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginationResult<InvoiceListViewModel>> GetAllAsync(int? month = null, int? year = null, int? status = null, int page = 1, int pageSize = 10)
    {
        var response = await _apiService.GetAsync<PaginationResult<InvoiceListViewModel>>($"invoices?month={month}&year={year}&status={status}&page={page}&pageSize={pageSize}");
        return response?.Data ?? new PaginationResult<InvoiceListViewModel>();
    }

    public async Task<InvoiceDetailViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<InvoiceDetailViewModel>($"invoices/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<object>?> UpdateAsync(Guid id, InvoiceEditViewModel model)
    {
        return await _apiService.PutAsync<object>($"invoices/{id}", model);
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(InvoiceCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("invoices", model);
    }
}
