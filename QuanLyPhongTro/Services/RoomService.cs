using QuanLyPhongTro.Models.ViewModels;

namespace QuanLyPhongTro.Services;

public class RoomService
{
    private readonly BaseApiService _apiService;

    public RoomService(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<PaginationResult<RoomListViewModel>> GetAllAsync(Guid? buildingId = null, int? status = null, string? keyword = null, int page = 1, int pageSize = 10)
    {
        var url = $"rooms?buildingId={buildingId}&status={status}&keyword={keyword}&page={page}&pageSize={pageSize}";
        var response = await _apiService.GetAsync<PaginationResult<RoomListViewModel>>(url);
        return response?.Data ?? new PaginationResult<RoomListViewModel>();
    }

    public async Task<RoomEditViewModel?> GetByIdAsync(Guid id)
    {
        var response = await _apiService.GetAsync<RoomEditViewModel>($"rooms/{id}");
        return response?.Data;
    }

    public async Task<RoomDetailViewModel?> GetDetailsAsync(Guid id)
    {
        var response = await _apiService.GetAsync<RoomDetailViewModel>($"rooms/{id}");
        return response?.Data;
    }

    public async Task<ApiResponse<Guid>?> CreateAsync(RoomCreateViewModel model)
    {
        return await _apiService.PostAsync<Guid>("rooms", model);
    }

    public async Task<ApiResponse<bool>?> UpdateAsync(Guid id, RoomEditViewModel model)
    {
        return await _apiService.PutAsync<bool>($"rooms/{id}", model);
    }

    public async Task<ApiResponse<bool>?> UpdateStatusAsync(Guid id, int status)
    {
        return await _apiService.PatchAsync<bool>($"rooms/{id}/status", new { status });
    }
}
