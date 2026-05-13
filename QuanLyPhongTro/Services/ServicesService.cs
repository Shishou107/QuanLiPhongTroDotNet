using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class ServicesService
{
    private readonly BaseApiService _apiService;

    public ServicesService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<ServiceListViewModel>> GetAllAsync()
    {
        var response = await _apiService.GetAsync<List<ServiceListViewModel>>("services");
        return response?.Data ?? new List<ServiceListViewModel>();
    }

    public async Task<ServiceEditViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<ServiceEditViewModel>($"services/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(ServiceCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("services", model);
    }

    public async Task<ApiResponse<bool>?> UpdateAsync(Guid id, ServiceEditViewModel model)
    {
        return await _apiService.PutAsync<bool>($"services/{id}", model);
    }
}
