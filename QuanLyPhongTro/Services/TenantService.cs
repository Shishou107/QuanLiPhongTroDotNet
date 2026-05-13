using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class TenantService
{
    private readonly BaseApiService _apiService;

    public TenantService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginationResult<TenantListViewModel>> GetAllAsync(string? keyword = null, int page = 1, int pageSize = 10)
    {
        var response = await _apiService.GetAsync<PaginationResult<TenantListViewModel>>($"tenants?keyword={keyword}&page={page}&pageSize={pageSize}");
        return response?.Data ?? new PaginationResult<TenantListViewModel>();
    }

    public async Task<TenantEditViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<TenantEditViewModel>($"tenants/{id}");
        return response?.Data;
    }

    public async Task<TenantDetailViewModel?> GetDetailsAsync(Guid id)
    {
        var response = await _apiService.GetAsync<TenantDetailViewModel>($"tenants/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(TenantCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("tenants", model);
    }

    public async Task<ApiResponse<bool>?> UpdateAsync(Guid id, TenantEditViewModel model)
    {
        return await _apiService.PutAsync<bool>($"tenants/{id}", model);
    }
}
