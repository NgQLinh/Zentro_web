using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    public class AccountController : Controller
    {
        private const string PendingUserKey = "PendingLogin.User";
        private const string PendingUserIdKey = "PendingLogin.UserId";
        private const string PendingRoleKey = "PendingLogin.Role";
        private const string ReturnUrlKey = "PendingLogin.ReturnUrl";
        private const string GenericLoginError = "Tài khoản hoặc mật khẩu không đúng";
        private readonly LoginProtectionService loginProtection;
        private readonly IConfiguration configuration;
        private readonly UserAccountService userAccountService;
        private readonly AuthenticationSettingsService authenticationSettings;
        private readonly UserActionLogService actionLogService;

        public AccountController(
            LoginProtectionService loginProtection,
            IConfiguration configuration,
            UserAccountService userAccountService,
            AuthenticationSettingsService authenticationSettings,
            UserActionLogService actionLogService)
        {
            this.loginProtection = loginProtection;
            this.configuration = configuration;
            this.userAccountService = userAccountService;
            this.authenticationSettings = authenticationSettings;
            this.actionLogService = actionLogService;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [Authorize]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            var clientKey = GetClientKey();
            if (loginProtection.IsLocked(clientKey) || !ModelState.IsValid)
            {
                if (loginProtection.IsLocked(clientKey))
                {
                    ModelState.Clear();
                    ModelState.AddModelError(string.Empty, GetLockoutMessage(clientKey));
                }
                return View(model);
            }

            var user = userAccountService.ValidateCredentials(model.Username, model.Password);
            if (user == null && !HasLegacyCredentials(model.Username, model.Password))
            {
                loginProtection.RegisterFailure(clientKey);
                actionLogService.Write(null, model.Username, "Unknown", "LOGIN", null, null, "Đăng nhập thất bại", false, GenericLoginError);
                ModelState.AddModelError(string.Empty, GenericLoginError);
                return View(model);
            }

            if (user != null)
            {
                HttpContext.Session.SetString(PendingUserKey, user.Username);
                HttpContext.Session.SetString(PendingUserIdKey, user.Id.ToString());
                HttpContext.Session.SetString(PendingRoleKey, user.Role);
            }
            else
            {
                HttpContext.Session.SetString(PendingUserKey, model.Username);
                HttpContext.Session.SetString(PendingUserIdKey, "0");
                HttpContext.Session.SetString(PendingRoleKey, "Admin");
            }

            HttpContext.Session.SetString(ReturnUrlKey, model.ReturnUrl ?? string.Empty);
            return RedirectToAction(nameof(Pin));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Pin()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(PendingUserKey)))
            {
                return RedirectToAction(nameof(Login));
            }

            return View(new PinViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pin(PinViewModel model)
        {
            var clientKey = GetClientKey();
            if (string.IsNullOrEmpty(HttpContext.Session.GetString(PendingUserKey)))
            {
                return RedirectToAction(nameof(Login));
            }

            var role = HttpContext.Session.GetString(PendingRoleKey) ?? "User";
            var configuredPin = authenticationSettings.GetPin(role);
            if (string.IsNullOrWhiteSpace(configuredPin))
            {
                configuredPin = role == "Admin" ? configuration["Authentication:AdminPin"] : configuration["Authentication:UserPin"];
            }
            var pinOk = string.IsNullOrWhiteSpace(configuredPin) || model.Pin == configuredPin;
            if (loginProtection.IsLocked(clientKey) || !ModelState.IsValid || !pinOk)
            {
                if (loginProtection.IsLocked(clientKey) || !pinOk)
                {
                    loginProtection.RegisterFailure(clientKey);
                    ModelState.Clear();
                    ModelState.AddModelError(string.Empty, loginProtection.IsLocked(clientKey)
                        ? GetLockoutMessage(clientKey)
                        : "Mã PIN không đúng");
                }

                return View(model);
            }

            var username = HttpContext.Session.GetString(PendingUserKey)!;
            var userId = HttpContext.Session.GetString(PendingUserIdKey) ?? "0";
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.NameIdentifier, userId),
                new("UserId", userId)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            var returnUrl = HttpContext.Session.GetString(ReturnUrlKey);
            HttpContext.Session.Clear();
            loginProtection.Reset(clientKey);
            actionLogService.Write(long.TryParse(userId, out var parsedUserId) && parsedUserId > 0 ? parsedUserId : null, username, role, "LOGIN", null, null, "Đăng nhập thành công", true);
            return RedirectToLocal(returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userIdValue = User.FindFirstValue("UserId");
            actionLogService.Write(long.TryParse(userIdValue, out var userId) ? userId : null, User.Identity?.Name ?? "unknown", User.FindFirstValue(ClaimTypes.Role) ?? "Unknown", "LOGOUT", null, null, "Đăng xuất", true);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Home");
        }

        private bool HasLegacyCredentials(string username, string password)
        {
            var configuredUsername = configuration["Authentication:Username"];
            var configuredPassword = configuration["Authentication:Password"];
            return !string.IsNullOrWhiteSpace(configuredUsername)
                && !string.IsNullOrWhiteSpace(configuredPassword)
                && username == configuredUsername
                && password == configuredPassword;
        }

        private string GetClientKey() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        private string GetLockoutMessage(string clientKey)
        {
            var remaining = loginProtection.GetRemainingLockout(clientKey);
            var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
            return $"Bạn đã nhập sai 3 lần. Tài khoản bị khóa trong 10 phút, còn khoảng {minutes} phút.";
        }
    }
}
