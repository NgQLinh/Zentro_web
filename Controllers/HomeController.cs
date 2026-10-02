using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    public class HomeController : Controller
    {
        private readonly NhaMayDataService nhaMayData;
        private readonly DashboardService dashboardService;
        private readonly PlcRuntimeState plcRuntimeState;

        public HomeController(NhaMayDataService nhaMayData, DashboardService dashboardService, PlcRuntimeState plcRuntimeState)
        {
            this.nhaMayData = nhaMayData;
            this.dashboardService = dashboardService;
            this.plcRuntimeState = plcRuntimeState;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";
            ViewData["Menu"] = "dashboard";
            ViewBag.PlcSimulationMode = plcRuntimeState.SimulationMode;
            var dashboardModel = dashboardService.GetDashboardData();
            return View(dashboardModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TogglePlcSimulation()
        {
            plcRuntimeState.SimulationMode = !plcRuntimeState.SimulationMode;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Device(string id)
        {
            var detail = dashboardService.GetDeviceDetail(id);
            if (string.IsNullOrWhiteSpace(detail.ThietBi.Ma))
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chi tiết {detail.ThietBi.Ten}";
            ViewData["Menu"] = "dashboard";
            return View("Device", detail);
        }

        [HttpGet]
        public IActionResult Status()
        {
            var state = nhaMayData.GetDashboard().HeThong;
            return Json(new
            {
                state.SoSanPham,
                state.SoLoi,
                state.TongSanPhamDat,
                state.AutoCycleStep,
                state.MotorDirection,
                state.TocDo,
                state.MotorRunning,
                state.Mode,
                state.TrangThai,
                state.TrangThaiMotor,
                state.CamBien,
                state.PlcOnline,
                state.NhietDo,
                state.XyLanhA,
                state.XyLanhB,
                state.BufferMax,
                state.DungKhanCap,
                state.LoiMay,
                state.BaoDay
            });
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult MaintenanceNotifications()
        {
            var notifications = nhaMayData.GetMaintenance()
                .Where(item => item.TrangThai is "ĐÃ LÊN LỊCH" or "ĐANG BẢO TRÌ")
                .Select(item => new
                {
                    item.ThietBi,
                    item.Loai,
                    item.NgayBt,
                    item.TrangThai,
                    item.GhiChu
                });

            return Json(notifications);
        }

        [HttpGet("api/dashboard/summary")]
        public IActionResult GetDashboardSummary()
        {
            var summary = dashboardService.GetProductionSummary();
            return Json(summary);
        }

        [HttpGet("api/dashboard/plc-status")]
        public IActionResult GetPlcStatus()
        {
            var status = dashboardService.GetPlcStatus();
            var machines = plcRuntimeState.Machines.Values.Select(snapshot => new
            {
                snapshot.MaMay,
                snapshot.Online,
                snapshot.LastError
            }).ToList();
            var offlineMachine = machines.FirstOrDefault(machine => !machine.Online);

            return Json(new
            {
                status.IsConnected,
                status.LastCommunication,
                status.SecondsSinceLastUpdate,
                status.IsStale,
                simulationMode = plcRuntimeState.SimulationMode,
                machines,
                lastError = offlineMachine == null
                    ? null
                    : $"{offlineMachine.MaMay}: {offlineMachine.LastError ?? "Không nhận được dữ liệu PLC"}"
            });
        }

        [HttpGet("api/dashboard/devices")]
        public IActionResult GetDevices()
        {
            var devices = dashboardService.GetDeviceStatus();
            return Json(devices);
        }

        [HttpGet("api/dashboard/sensors")]
        public IActionResult GetSensors()
        {
            var sensors = dashboardService.GetSensorStatus();
            return Json(sensors);
        }

        [HttpGet("api/dashboard/alarms")]
        public IActionResult GetAlarms(int limit = 10)
        {
            var alarms = dashboardService.GetRecentAlarms(limit);
            return Json(alarms);
        }

        [HttpGet("api/dashboard/full")]
        public IActionResult GetFullDashboard()
        {
            var dashboard = dashboardService.GetDashboardData();
            return Json(dashboard);
        }

        [HttpGet("api/dashboard/export/json")]
        public IActionResult ExportDashboardJson()
        {
            var dashboard = dashboardService.GetDashboardData();
            var json = System.Text.Json.JsonSerializer.Serialize(dashboard, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"dashboard-{DateTime.Now:yyyy-MM-dd-HHmmss}.json");
        }

        [HttpGet("api/dashboard/export/csv")]
        public IActionResult ExportDashboardCsv()
        {
            var dashboard = dashboardService.GetDashboardData();
            var csv = new System.Text.StringBuilder();
            
            csv.AppendLine("DASHBOARD REPORT - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            csv.AppendLine();
            
            csv.AppendLine("=== PRODUCTION SUMMARY ===");
            csv.AppendLine($"Total Production,{dashboard.SanXuat.TongSanLuong}");
            csv.AppendLine($"OK Products,{dashboard.SanXuat.SanLuongOK}");
            csv.AppendLine($"NG Products,{dashboard.SanXuat.SanLuongNG}");
            csv.AppendLine($"OK Ratio,{dashboard.SanXuat.TyLeOK:F2}%");
            csv.AppendLine($"Current Product,{dashboard.SanXuat.SanPhamHienTai}");
            csv.AppendLine($"Current Lot,{dashboard.SanXuat.LoHienTai}");
            csv.AppendLine();
            
            csv.AppendLine("=== DEVICE STATUS ===");
            csv.AppendLine("Device ID,Device Name,Type,Status,Speed");
            foreach (var device in dashboard.ThietBi)
            {
                csv.AppendLine($"{device.Ma},{device.Ten},{device.Loai},{device.TrangThai},{device.TocDo}%");
            }
            csv.AppendLine();
            
            csv.AppendLine("=== SENSOR STATUS ===");
            csv.AppendLine("Sensor ID,Sensor Name,Type,Value,Unit,Status");
            foreach (var sensor in dashboard.CamBien)
            {
                csv.AppendLine($"{sensor.Ma},{sensor.Ten},{sensor.Loai},{sensor.GiaTri},{sensor.DonVi},{(sensor.IsActive ? "ACTIVE" : "INACTIVE")}");
            }
            csv.AppendLine();
            
            csv.AppendLine("=== RECENT ALARMS ===");
            csv.AppendLine("Severity,Device,Description,Time,Status");
            foreach (var alarm in dashboard.BaoDong.Take(10))
            {
                csv.AppendLine($"{alarm.MucDo},{alarm.ThietBi},{alarm.NoiDung},{alarm.NgayGio:HH:mm:ss},{alarm.TrangThai}");
            }
            csv.AppendLine();
            
            csv.AppendLine("=== MAINTENANCE SCHEDULE ===");
            csv.AppendLine("Device,Type,Last Service,Next Service,Status");
            foreach (var maint in dashboard.BaoTri)
            {
                csv.AppendLine($"{maint.ThietBi},{maint.Loai},{maint.NgayBt},{maint.LanTiepTheo},{maint.TrangThai}");
            }
            
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"dashboard-{DateTime.Now:yyyy-MM-dd-HHmmss}.csv");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
