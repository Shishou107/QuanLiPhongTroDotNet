using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class BuildingService
{
    private readonly BaseApiService _apiService;

    public BuildingService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<BuildingListViewModel>> GetAllAsync(string? keyword = null)
    {
        var response = await _apiService.GetAsync<List<BuildingListViewModel>>($"buildings?keyword={keyword}");
        return response?.Data ?? new List<BuildingListViewModel>();
    }

    public async Task<BuildingEditViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<BuildingEditViewModel>($"buildings/{id}");
        return response?.Data;
    }

    public async Task<BuildingDetailViewModel?> GetDetailsAsync(Guid id)
    {
        var response = await _apiService.GetAsync<BuildingDetailViewModel>($"buildings/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(BuildingCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("buildings", model);
    }

    public async Task<ApiResponse<bool>?> UpdateAsync(Guid id, BuildingEditViewModel model)
    {
        return await _apiService.PutAsync<bool>($"buildings/{id}", model);
    }

    public async Task<ApiResponse<bool>?> DeleteAsync(Guid id)
    {
        return await _apiService.DeleteAsync($"buildings/{id}");
    }
}
