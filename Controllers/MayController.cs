using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MayController : Controller
    {
        private readonly MachineService machineService;
        private readonly PlcRuntimeState plcState;

        public MayController(MachineService machineService, PlcRuntimeState plcState)
        {
            this.machineService = machineService;
            this.plcState = plcState;
        }

        public IActionResult Index(string? maMay = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            ViewData["Title"] = "Quản lý máy";
            ViewData["Menu"] = "may";
            var machines = machineService.GetAll();
            foreach (var machine in machines)
            {
                if (plcState.Machines.TryGetValue(machine.MaMay, out var snap))
                {
                    machine.PlcOnline = snap.Online;
                }
            }

            var history = machineService.GetStatusHistory(maMay, fromDate, toDate, 200);
            var now = DateTime.Now;
            var runningSeconds = history.Where(item => item.TrangThaiMoi == "RUNNING").Sum(item => item.ThoiLuongGiay ?? Math.Max(0, (int)(now - item.ThoiDiem).TotalSeconds));
            var stoppedSeconds = history.Where(item => item.TrangThaiMoi is "STOPPED" or "PAUSED").Sum(item => item.ThoiLuongGiay ?? Math.Max(0, (int)(now - item.ThoiDiem).TotalSeconds));
            var alarmSeconds = history.Where(item => item.TrangThaiMoi == "ALARM").Sum(item => item.ThoiLuongGiay ?? Math.Max(0, (int)(now - item.ThoiDiem).TotalSeconds));
            var downtimeReasons = history
                .Where(item => item.TrangThaiMoi is "STOPPED" or "PAUSED")
                .GroupBy(item => string.IsNullOrWhiteSpace(item.LyDoDung) ? "Không rõ" : item.LyDoDung)
                .ToDictionary(group => group.Key!, group => (Count: group.Count(), Seconds: group.Sum(item => item.ThoiLuongGiay ?? Math.Max(0, (int)(now - item.ThoiDiem).TotalSeconds))));
            ViewBag.History = history;
            ViewBag.HistoryMachine = maMay;
            ViewBag.HistoryFrom = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.HistoryTo = toDate?.ToString("yyyy-MM-dd");
            ViewBag.RunningSeconds = runningSeconds;
            ViewBag.StoppedSeconds = stoppedSeconds;
            ViewBag.AlarmSeconds = alarmSeconds;
            ViewBag.DowntimeReasons = downtimeReasons;
            return View(machines);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Thêm máy";
            ViewData["Menu"] = "may";
            return View("Edit", new MachineEditViewModel { IsNew = true });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var machine = machineService.Get(id);
            if (machine == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Sửa máy";
            ViewData["Menu"] = "may";
            return View(new MachineEditViewModel
            {
                IsNew = false,
                MaMay = machine.MaMay,
                Ten = machine.Ten,
                IpPlc = machine.IpPlc,
                PortPlc = machine.PortPlc,
                IsActive = machine.IsActive,
                BangTaiMap = machine.BangTaiMap,
                TrangThai = machine.TrangThai
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MachineEditViewModel model)
        {
            ViewData["Title"] = model.IsNew ? "Thêm máy" : "Sửa máy";
            ViewData["Menu"] = "may";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                machineService.Save(new MachineRecord
                {
                    MaMay = model.MaMay.Trim(),
                    Ten = model.Ten.Trim(),
                    IpPlc = model.IpPlc?.Trim(),
                    PortPlc = model.PortPlc,
                    IsActive = model.IsActive,
                    BangTaiMap = string.IsNullOrWhiteSpace(model.BangTaiMap) ? null : model.BangTaiMap,
                    TrangThai = string.IsNullOrWhiteSpace(model.TrangThai) ? "STOPPED" : model.TrangThai
                }, model.IsNew);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id)
        {
            try
            {
                machineService.Delete(id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetStatus(string id, string status)
        {
            try
            {
                machineService.SetStatus(id, status, "UI");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult StatusApi()
        {
            var machines = machineService.GetAll(activeOnly: true).Select(item =>
            {
                plcState.Machines.TryGetValue(item.MaMay, out var snap);
                return new
                {
                    item.MaMay,
                    item.Ten,
                    item.TrangThai,
                    item.IpPlc,
                    item.PortPlc,
                    PlcOnline = snap?.Online ?? item.PlcOnline,
                    Temperature = snap?.Temperature,
                    LastCommunication = snap?.LastCommunication
                };
            });
            return Json(machines);
        }

        [HttpGet]
        public IActionResult ExportHistory(string? maMay = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var history = machineService.GetStatusHistory(maMay, fromDate, toDate, 5000);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("LichSuMay");
            var headers = new[] { "Máy", "Trạng thái cũ", "Trạng thái mới", "Bắt đầu", "Kết thúc", "Thời lượng (giây)", "Lý do dừng", "Người thực hiện", "Nguồn", "Ghi chú" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
            for (var row = 0; row < history.Count; row++)
            {
                var item = history[row];
                sheet.Cell(row + 2, 1).Value = item.MaMay;
                sheet.Cell(row + 2, 2).Value = item.TrangThaiCu;
                sheet.Cell(row + 2, 3).Value = item.TrangThaiMoi;
                sheet.Cell(row + 2, 4).Value = item.ThoiDiem;
                sheet.Cell(row + 2, 5).Value = item.ThoiDiemKetThuc;
                sheet.Cell(row + 2, 6).Value = item.ThoiLuongGiay;
                sheet.Cell(row + 2, 7).Value = item.LyDoDung;
                sheet.Cell(row + 2, 8).Value = item.NguoiThucHien;
                sheet.Cell(row + 2, 9).Value = item.Nguon;
                sheet.Cell(row + 2, 10).Value = item.GhiChu;
            }
            sheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"lich-su-may-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}
