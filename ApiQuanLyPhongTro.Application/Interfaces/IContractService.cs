using System;
using System.Threading.Tasks;
using ApiQuanLyPhongTro.Application.DTO;
using ApiQuanLyPhongTro.Application.Common;

namespace ApiQuanLyPhongTro.Application.Interfaces;

public interface IContractService
{
    Task<ApiResponse<Guid>> CreateContractAsync(CreateContractDto dto);
    Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> CancelContractAsync(Guid contractId);
    Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> FinishContractAsync(Guid contractId);
    Task<ApiQuanLyPhongTro.Application.Common.ApiResponse> UpdateContractAsync(Guid id, UpdateContractDto dto);
}
