using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin,User")]
    public class BangTaiController : Controller
    {
        private readonly BangTaiModel trangThai;
        private readonly BangTaiServices bangTaiServices;
        private readonly BangTaiLine2Model trangThai2;
        private readonly BangTaiLine2Services bangTaiLine2Services;
        private readonly PhanLoaiServices phanLoaiServices;
        private readonly PhanLoaiLine2Services phanLoaiLine2Services;
        private readonly UserAccountService userAccountService;
        private readonly MachineService machineService;

        public BangTaiController(
            BangTaiModel trangThai,
            BangTaiServices bangTaiServices,
            BangTaiLine2Model trangThai2,
            BangTaiLine2Services bangTaiLine2Services,
            PhanLoaiServices phanLoaiServices,
            PhanLoaiLine2Services phanLoaiLine2Services,
            UserAccountService userAccountService,
            MachineService machineService)
        {
            this.trangThai = trangThai;
            this.bangTaiServices = bangTaiServices;
            this.trangThai2 = trangThai2;
            this.bangTaiLine2Services = bangTaiLine2Services;
            this.phanLoaiServices = phanLoaiServices;
            this.phanLoaiLine2Services = phanLoaiLine2Services;
            this.userAccountService = userAccountService;
            this.machineService = machineService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(DieuKhien));
        }

        public IActionResult DieuKhien(string? page = null)
        {
            if (!IsAssigned("BT01")) return Forbid();
            if (page == "PhanLoai")
            {
                ViewData["Title"] = "Trạm phân loại";
                ViewData["Menu"] = "phanloai";
                return View("PhanLoai", trangThai);
            }

            ViewData["Title"] = "Điều khiển băng tải";
            ViewData["Menu"] = "bangtai";
            ViewBag.HasConveyor2 = HasAssignedConveyor("BT02");
            return View(trangThai);
        }

        public IActionResult DieuKhien2()
        {
            if (!IsAssigned("BT02"))
            {
                TempData["Error"] = "Bạn chưa được Admin phân công Băng tải 2.";
                return RedirectToAction(nameof(DieuKhien));
            }
            ViewData["Title"] = "Điều khiển băng tải 02";
            ViewData["Menu"] = "bangtai2";
            ViewData["ConveyorNumber"] = "02";
            return View("DieuKhien", trangThai2);
        }

        public IActionResult PhanLoaiPage()
        {
            if (!IsAssigned("BT01")) return Forbid();
            ViewData["Title"] = "Trạm phân loại";
            ViewData["Menu"] = "phanloai";
            return View("PhanLoai", trangThai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetModeAuto(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).SetModeAuto();
            SyncMachineStatus(returnPage);
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetModeManual(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).SetModeManual();
            SyncMachineStatus(returnPage, "Chuyển sang MANUAL");
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetAuto(string? returnPage = null) => SetModeAuto(returnPage);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetManual(string? returnPage = null) => SetModeManual(returnPage);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetSpeed(int tocDo, string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).SetSpeed(tocDo);
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(string? returnPage = null, string? loai = null, string? command = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            var normalizedCommand = command switch
            {
                "AUTO_LINE" => "AUTO",
                "MANUAL_LINE" => "MANUAL",
                "STOP_LINE" => "STOP",
                "RESET_LINE" => "RESET",
                "E_STOP_LINE" => "E_STOP",
                "FAULT_LINE" => "FAULT",
                "START_LINE" => "START",
                _ => command
            };

            if (!string.IsNullOrWhiteSpace(loai))
            {
                await GetPhanLoaiServices(returnPage).PhanLoai(loai);
            }
            else if (normalizedCommand == "AUTO")
            {
                GetServices(returnPage).SetModeAuto();
            }
            else if (normalizedCommand == "MANUAL")
            {
                GetServices(returnPage).SetModeManual();
            }
            else if (normalizedCommand == "STOP")
            {
                GetServices(returnPage).Stop();
            }
            else if (normalizedCommand == "RESET")
            {
                GetServices(returnPage).Reset();
            }
            else if (normalizedCommand == "E_STOP")
            {
                GetServices(returnPage).EmergencyStop();
            }
            else if (normalizedCommand == "FAULT")
            {
                GetServices(returnPage).BaoLoiMay();
            }
            else if (normalizedCommand == "LUI" || normalizedCommand == "TIEN")
            {
                await GetServices(returnPage).MoveManual(normalizedCommand);
            }
            else
            {
                GetServices(returnPage).Start();
            }

            SyncMachineStatus(returnPage, normalizedCommand is "STOP" or "E_STOP" or "FAULT" ? "Thao tác vận hành" : null);

            if (command?.EndsWith("_LINE", StringComparison.Ordinal) == true)
            {
                return RedirectToRoute("conveyorControl");
            }

            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Stop(string? returnPage = null, string? lyDoDung = null, string? ghiChuDung = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).Stop();
            SyncMachineStatus(returnPage, lyDoDung ?? "Người vận hành dừng máy", ghiChuDung);
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Tien(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            await GetServices(returnPage).MoveManual("TIEN");
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lui(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            await GetServices(returnPage).MoveManual("LUI");
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EmergencyStop(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).EmergencyStop();
            SyncMachineStatus(returnPage, "Dừng khẩn cấp");
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BaoLoi(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).BaoLoiMay();
            SyncMachineStatus(returnPage, "Báo lỗi máy");
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reset(string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            GetServices(returnPage).Reset();
            SyncMachineStatus(returnPage, "RESET máy");
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XyLanhAOn()
        {
            if (!IsAssigned("BT01")) return Forbid();
            bangTaiServices.SetCylinder("A", true);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XyLanhAOff()
        {
            if (!IsAssigned("BT01")) return Forbid();
            bangTaiServices.SetCylinder("A", false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XyLanhBOn()
        {
            if (!IsAssigned("BT01")) return Forbid();
            bangTaiServices.SetCylinder("B", true);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XyLanhBOff()
        {
            if (!IsAssigned("BT01")) return Forbid();
            bangTaiServices.SetCylinder("B", false);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhanLoai(string loai, string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            await phanLoaiServices.PhanLoai(loai);
            return RedirectAfterAction(returnPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulateSensor(string loai)
        {
            if (!IsAssigned("BT01")) return Forbid();
            await phanLoaiServices.PhanLoai(loai);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhatHienSanPham(string? loai = null, string? returnPage = null)
        {
            if (!IsAssigned(returnPage == "DieuKhien2" ? "BT02" : "BT01")) return Forbid();
            await GetPhanLoaiServices(returnPage).PhanLoai(loai ?? BangTaiModel.LoaiLinhKien[0]);
            return RedirectAfterAction(returnPage);
        }

        private IActionResult RedirectAfterAction(string? returnPage)
        {
            return returnPage switch
            {
                "PhanLoai" => RedirectToRoute("sorting"),
                "DieuKhien2" => RedirectToAction(nameof(DieuKhien2)),
                _ => RedirectToRoute("conveyorControl")
            };
        }

        [HttpGet]
        public IActionResult Status2()
        {
            return Json(new
            {
                trangThai2.SoSanPham,
                trangThai2.AutoCycleStep,
                trangThai2.MotorDirection,
                trangThai2.MotorRunning,
                trangThai2.TrangThai,
                trangThai2.TrangThaiMotor,
                trangThai2.CamBien,
                trangThai2.PlcOnline,
                trangThai2.NhietDo,
                trangThai2.XyLanhA,
                trangThai2.XyLanhB,
                trangThai2.BufferMax,
                trangThai2.DungKhanCap,
                trangThai2.LoiMay,
                trangThai2.BaoDay
            });
        }

        private BangTaiServices GetServices(string? returnPage)
        {
            return returnPage == "DieuKhien2" ? bangTaiLine2Services : bangTaiServices;
        }

        private PhanLoaiServices GetPhanLoaiServices(string? returnPage)
        {
            return returnPage == "DieuKhien2" ? phanLoaiLine2Services : phanLoaiServices;
        }

        private void SyncMachineStatus(string? returnPage, string? reason = null, string? note = null)
        {
            var conveyorCode = returnPage == "DieuKhien2" ? "BT02" : "BT01";
            var machineCode = machineService.GetAll(activeOnly: true)
                .FirstOrDefault(item => string.Equals(item.BangTaiMap, conveyorCode, StringComparison.OrdinalIgnoreCase))?.MaMay;
            if (string.IsNullOrWhiteSpace(machineCode))
            {
                return;
            }

            var state = returnPage == "DieuKhien2" ? trangThai2 : trangThai;
            var status = state.State switch
            {
                BangTaiState.RUN or BangTaiState.PROCESS => "RUNNING",
                BangTaiState.MANUAL => "PAUSED",
                BangTaiState.ALARM => "ALARM",
                _ => "STOPPED"
            };
            machineService.SetStatus(machineCode, status, "UI", note ?? state.TrangThai, reason, User.Identity?.Name);
        }

        private bool IsAssigned(string conveyorCode)
        {
            if (User.IsInRole("Admin")) return true;

            var userIdValue = User.FindFirstValue("UserId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdValue, out var userId)) return false;
            var assigned = userAccountService.GetAssignedMachines(userId);
            var machineCodes = machineService.GetAll(activeOnly: true)
                .Where(item => string.Equals(item.BangTaiMap, conveyorCode, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.MaMay);
            return machineCodes.Any(assigned.Contains);
        }

        public bool HasAssignedConveyor(string conveyorCode) => IsAssigned(conveyorCode);
    }
}
