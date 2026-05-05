using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CKCNNET.Services;
using CKCNNET.Models;

namespace CKCNNET.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string confirmPassword, string phoneNumber)
        {
            try
            {
                if (password != confirmPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu không khớp!");
                    return View();
                }

                await _authService.RegisterAsync(username, email, password, phoneNumber);
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            try
            {
                var user = await _authService.LoginAsync(email, password);

                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserRole", user.Role.Name);
                HttpContext.Session.SetString("UserEmail", user.Email);

                if (rememberMe)
                {
                    Response.Cookies.Append("UserId", user.Id.ToString(),
                        new Microsoft.AspNetCore.Http.CookieOptions { Expires = DateTime.Now.AddDays(30) });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View();
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}