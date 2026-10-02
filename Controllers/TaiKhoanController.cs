using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TaiKhoanController : Controller
    {
        private const string AdminPinVerifiedKey = "PinChange.AdminVerified";
        private const string UserPinVerifiedKey = "PinChange.UserVerified";
        private readonly UserAccountService userAccountService;
        private readonly MachineService machineService;
        private readonly AuthenticationSettingsService authenticationSettings;
        private readonly UserActionLogService actionLogService;

        public TaiKhoanController(UserAccountService userAccountService, MachineService machineService, AuthenticationSettingsService authenticationSettings, UserActionLogService actionLogService)
        {
            this.userAccountService = userAccountService;
            this.machineService = machineService;
            this.authenticationSettings = authenticationSettings;
            this.actionLogService = actionLogService;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Tài khoản";
            ViewData["Menu"] = "taikhoan";
            return View(userAccountService.GetAll());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Thêm tài khoản";
            ViewData["Menu"] = "taikhoan";
            return View("Edit", new TaiKhoanEditViewModel
            {
                IsNew = true,
                AllMachines = machineService.GetAll(activeOnly: true)
            });
        }

        [HttpGet]
        public IActionResult Edit(long id)
        {
            var user = userAccountService.GetById(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Sửa tài khoản";
            ViewData["Menu"] = "taikhoan";
            return View(new TaiKhoanEditViewModel
            {
                IsNew = false,
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive,
                SelectedMachines = userAccountService.GetAssignedMachines(user.Id),
                AllMachines = machineService.GetAll(activeOnly: true)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TaiKhoanEditViewModel model)
        {
            ViewData["Menu"] = "taikhoan";
            model.AllMachines = machineService.GetAll(activeOnly: true);
            model.SelectedMachines ??= new List<string>();
            if (model.IsNew && string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Mật khẩu bắt buộc khi tạo mới.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (model.IsNew)
                {
                    userAccountService.Create(model.Username.Trim(), model.Password!, model.Role, model.SelectedMachines);
                    actionLogService.Write(GetUserId(), User.Identity?.Name ?? "unknown", "Admin", "ACCOUNT_CREATE", null, null, $"Tạo tài khoản {model.Username.Trim()} ({model.Role})", true);
                }
                else
                {
                    userAccountService.Update(model.Id, model.Role, model.IsActive, model.Password, model.SelectedMachines);
                    actionLogService.Write(GetUserId(), User.Identity?.Name ?? "unknown", "Admin", "ACCOUNT_UPDATE", null, null, $"Cập nhật tài khoản {model.Username}", true);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            try
            {
                userAccountService.Delete(id);
                actionLogService.Write(GetUserId(), User.Identity?.Name ?? "unknown", "Admin", "ACCOUNT_DELETE", null, null, $"Xóa tài khoản {id}", true);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private long? GetUserId()
        {
            return long.TryParse(User.FindFirst("UserId")?.Value, out var userId) ? userId : null;
        }

        [HttpGet]
        public IActionResult Pin(string? role = null, bool verified = false)
        {
            ViewData["Title"] = "Mã PIN hệ thống";
            ViewData["Menu"] = "taikhoan";

            var model = authenticationSettings.GetSettings();
            if (role == "Admin")
            {
                model.ShowAdminNewPin = verified && HttpContext.Session.GetString(AdminPinVerifiedKey) == "true";
            }
            if (role == "User")
            {
                model.ShowUserNewPin = verified && HttpContext.Session.GetString(UserPinVerifiedKey) == "true";
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOldPin(string role, string oldPin)
        {
            if (role is not ("Admin" or "User"))
            {
                return BadRequest();
            }

            var currentPin = authenticationSettings.GetPin(role);
            if (oldPin == currentPin)
            {
                HttpContext.Session.SetString(role == "Admin" ? AdminPinVerifiedKey : UserPinVerifiedKey, "true");
                return RedirectToAction(nameof(Pin), new { role, verified = true });
            }

            TempData["Error"] = $"PIN {role} cũ không đúng.";
            return RedirectToAction(nameof(Pin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pin(PinSettingsViewModel model)
        {
            ViewData["Title"] = "Mã PIN hệ thống";
            ViewData["Menu"] = "taikhoan";

            var changingAdminPin = !string.IsNullOrWhiteSpace(model.AdminPin);
            var changingUserPin = !string.IsNullOrWhiteSpace(model.UserPin);
            var adminPinVerified = HttpContext.Session.GetString(AdminPinVerifiedKey) == "true";
            var userPinVerified = HttpContext.Session.GetString(UserPinVerifiedKey) == "true";

            if (!changingAdminPin && !changingUserPin)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập PIN mới.");
            }

            if (changingAdminPin && !adminPinVerified)
            {
                TempData["Error"] = "Phiên xác minh PIN Admin đã hết hạn. Hãy xác nhận PIN cũ lại.";
                return RedirectToAction(nameof(Pin));
            }

            if (changingUserPin && !userPinVerified)
            {
                TempData["Error"] = "Phiên xác minh PIN User đã hết hạn. Hãy xác nhận PIN cũ lại.";
                return RedirectToAction(nameof(Pin));
            }

            if (!ModelState.IsValid)
            {
                model.ShowAdminNewPin = changingAdminPin;
                model.ShowUserNewPin = changingUserPin;
                return View(model);
            }

            authenticationSettings.Save(model);
            if (changingAdminPin)
            {
                HttpContext.Session.Remove(AdminPinVerifiedKey);
            }
            if (changingUserPin)
            {
                HttpContext.Session.Remove(UserPinVerifiedKey);
            }

            TempData["Success"] = changingAdminPin && changingUserPin
                ? "Đã cập nhật PIN Admin và User."
                : changingAdminPin ? "Đã cập nhật PIN Admin." : "Đã cập nhật PIN User.";
            return RedirectToAction(nameof(Pin));
        }
    }
}
