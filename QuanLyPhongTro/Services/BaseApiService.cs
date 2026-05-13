using System.Net.Http.Json;
using System.Text.Json;

namespace QuanLyPhongTro.Services;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}

public class BaseApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public BaseApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private HttpClient CreateClient() => _httpClientFactory.CreateClient("QuanLyPhongTroApi");

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        var client = CreateClient();
        return await client.GetFromJsonAsync<ApiResponse<T>>(endpoint, _jsonOptions);
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object data)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync(endpoint, data);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data)
    {
        var client = CreateClient();
        var response = await client.PutAsJsonAsync(endpoint, data);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
    }

    public async Task<ApiResponse<bool>?> DeleteAsync(string endpoint)
    {
        var client = CreateClient();
        var response = await client.DeleteAsync(endpoint);
        return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(_jsonOptions);
    }

    public async Task<ApiResponse<T>?> PatchAsync<T>(string endpoint, object data)
    {
        var client = CreateClient();
        var response = await client.PatchAsJsonAsync(endpoint, data);
        return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
    }
}
