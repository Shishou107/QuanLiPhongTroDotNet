using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using QuanLyPhongTro.Models.ViewModels;
using QuanLyPhongTro.Services;
using System.Security.Claims;

namespace QuanLyPhongTro.Controllers;

public class AccountController : Controller
{
    private readonly BaseApiService _apiService;

    public AccountController(BaseApiService apiService)
    {
        _apiService = apiService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        if (ModelState.IsValid)
        {
            var result = await _apiService.PostAsync<LoginResultViewModel>("account/login", new
            {
                model.Username,
                model.Password
            });

            if (result != null && result.Success && result.Data != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.Data.Id.ToString()),
                    new Claim(ClaimTypes.Name, result.Data.FullName),
                    new Claim("Username", result.Data.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError("", result?.Message ?? "Tên đăng nhập hoặc mật khẩu không đúng");
        }

        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }
}
