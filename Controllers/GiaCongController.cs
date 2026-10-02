using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "User")]
    public class GiaCongController : Controller
    {
        private readonly MachiningService machiningService;
        private readonly MachineService machineService;
        private readonly PartService partService;
        private readonly UserAccountService userAccountService;
        private readonly UserActionLogService actionLogService;

        public GiaCongController(
            MachiningService machiningService,
            MachineService machineService,
            PartService partService,
            UserAccountService userAccountService,
            UserActionLogService actionLogService)
        {
            this.machiningService = machiningService;
            this.machineService = machineService;
            this.partService = partService;
            this.userAccountService = userAccountService;
            this.actionLogService = actionLogService;
        }

        public IActionResult Index(string? maMay = null)
        {
            ViewData["Title"] = "Gia công";
            ViewData["Menu"] = "giacong";
            return View(BuildViewModel(maMay));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Start(string maMay, long partId, string? lot)
        {
            ViewData["Title"] = "Gia công";
            ViewData["Menu"] = "giacong";
            var userId = GetUserId();
            var username = User.Identity?.Name ?? "unknown";
            if (User.IsInRole("User") && !userAccountService.GetAssignedMachines(userId).Contains(maMay))
            {
                actionLogService.Write(userId, username, "User", "MACHINING_START", maMay, null, "Bắt đầu gia công bị từ chối", false, "Máy chưa được phân công");
                var denied = BuildViewModel(maMay);
                denied.Message = "Bạn không được phân công máy này.";
                denied.IsError = true;
                return View("Index", denied);
            }

            var result = machiningService.Start(maMay, partId, userId, username, lot);
            actionLogService.Write(userId, username, "User", "MACHINING_START", maMay, result.Record?.MaIndex, result.Message, result.Ok, result.Ok ? null : result.Message);
            var model = BuildViewModel(maMay);
            model.Message = result.Message;
            model.IsError = !result.Ok;
            model.SelectedPartId = partId;
            model.Lot = lot;
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult End(long id, string ketQua, string? maMay)
        {
            var userId = GetUserId();
            if (!string.IsNullOrWhiteSpace(maMay) && !userAccountService.GetAssignedMachines(userId).Contains(maMay, StringComparer.OrdinalIgnoreCase))
            {
                var denied = BuildViewModel(maMay);
                denied.Message = "Bạn không được phân công máy này.";
                denied.IsError = true;
                return View("Index", denied);
            }

            var result = machiningService.End(id, string.IsNullOrWhiteSpace(ketQua) ? "OK" : ketQua, userId);
            actionLogService.Write(userId, User.Identity?.Name ?? "unknown", "User", "MACHINING_END", maMay, null, result.Message, result.Ok, result.Ok ? null : result.Message);
            var model = BuildViewModel(maMay);
            model.Message = result.Message;
            model.IsError = !result.Ok;
            return View("Index", model);
        }

        private GiaCongViewModel BuildViewModel(string? maMay)
        {
            var userId = GetUserId();
            var allMachines = machineService.GetAll(activeOnly: true);
            var assignedMachines = userAccountService.GetAssignedMachines(userId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var selected = string.IsNullOrWhiteSpace(maMay)
                ? allMachines.FirstOrDefault(item => assignedMachines.Contains(item.MaMay))?.MaMay ?? allMachines.FirstOrDefault()?.MaMay
                : maMay;
            var history = User.IsInRole("User") && !User.IsInRole("Admin")
                ? machiningService.GetHistory(maMay: selected, userId: userId)
                : machiningService.GetHistory(maMay: selected);

            return new GiaCongViewModel
            {
                Machines = allMachines,
                AssignedMachines = assignedMachines,
                Parts = partService.GetAll(maMay: selected),
                History = history,
                ActiveJob = string.IsNullOrWhiteSpace(selected) ? null : machiningService.GetActiveJob(selected),
                SelectedMaMay = selected
            };
        }

        private long GetUserId()
        {
            var claim = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(claim, out var id) ? id : 0;
        }
    }
}
