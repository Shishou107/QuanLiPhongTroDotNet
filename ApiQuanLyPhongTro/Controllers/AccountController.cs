using System.Data;
using System.Security.Cryptography;
using System.Text;
using ApiQuanLyPhongTro.Application.Common;
using ApiQuanLyPhongTro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly AdoNetDb _db;

    public AccountController(AdoNetDb db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        await using var connection = _db.CreateConnection();
        await using var command = _db.CreateStoredProcedureCommand(connection, "sp_Account_GetByLoginName");
        command.Parameters.Add("@LoginName", SqlDbType.NVarChar, 100).Value = dto.Username.Trim();

        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return Unauthorized(ApiResponse.FailureResult("Tên đăng nhập hoặc mật khẩu không đúng"));
        }

        var account = new
        {
            Id = reader.GetInt32(reader.GetOrdinal("MaTK")),
            Username = reader.GetStringValue("TenDangNhap"),
            PasswordHash = reader.GetStringValue("MatKhauHash"),
            FullName = reader.GetStringValue("HoTen"),
            Email = reader.GetNullableStringValue("Email"),
            IsActive = reader.GetBoolean(reader.GetOrdinal("TrangThai"))
        };

        if (!account.IsActive || !VerifyPassword(dto.Password, account.PasswordHash))
        {
            return Unauthorized(ApiResponse.FailureResult("Tên đăng nhập hoặc mật khẩu không đúng"));
        }

        await reader.CloseAsync();

        await using var updateCommand = _db.CreateStoredProcedureCommand(connection, "sp_Account_UpdateLastLogin");
        updateCommand.Parameters.Add("@Id", SqlDbType.Int).Value = account.Id;
        await updateCommand.ExecuteNonQueryAsync();

        var result = new LoginResultDto
        {
            Id = account.Id,
            Username = account.Username,
            FullName = account.FullName,
            Email = account.Email
        };

        return Ok(ApiResponse<LoginResultDto>.SuccessResult(result, "Đăng nhập thành công"));
    }

    private static bool VerifyPassword(string password, string storedPassword)
    {
        if (storedPassword == password)
        {
            return true;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var hash = Convert.ToHexString(hashBytes);

        return string.Equals(storedPassword, hash, StringComparison.OrdinalIgnoreCase);
    }
}

public class LoginRequestDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResultDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
}
