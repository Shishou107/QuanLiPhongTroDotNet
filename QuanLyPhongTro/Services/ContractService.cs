using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class ContractService
{
    private readonly BaseApiService _apiService;

    public ContractService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginationResult<ContractListViewModel>> GetAllAsync(string? keyword = null, int? status = null, int page = 1, int pageSize = 10)
    {
        var response = await _apiService.GetAsync<PaginationResult<ContractListViewModel>>($"contracts?keyword={keyword}&status={status}&page={page}&pageSize={pageSize}");
        return response?.Data ?? new PaginationResult<ContractListViewModel>();
    }

    public async Task<ContractDetailViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<ContractDetailViewModel>($"contracts/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<object>?> UpdateAsync(Guid id, ContractEditViewModel model)
    {
        return await _apiService.PutAsync<object>($"contracts/{id}", model);
    }

    public async Task<ApiResponse<Guid>?> CreateContractAsync(ContractCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("contracts", model);
    }
}
