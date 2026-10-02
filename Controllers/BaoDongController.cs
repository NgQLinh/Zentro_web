using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using Zentro.Data;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin,User,Maintenance")]
    public class BaoDongController : Controller
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly MachineService machineService;
        private readonly BangTaiModel heThong;

        public BaoDongController(
            IDbContextFactory<ProductionDbContext> contextFactory,
            MachineService machineService,
            BangTaiModel heThong)
        {
            this.contextFactory = contextFactory;
            this.machineService = machineService;
            this.heThong = heThong;
        }

        public IActionResult Index(string? maMay = null, string? loai = null, string? trangThai = null)
        {
            ViewData["Title"] = "Báo động & sự cố";
            ViewData["Menu"] = "baodong";
            using var db = contextFactory.CreateDbContext();
            var query = db.AlarmRecords.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(maMay) && maMay != "Tất cả")
            {
                query = query.Where(item => item.ThietBi == maMay);
            }

            if (!string.IsNullOrWhiteSpace(loai) && loai != "Tất cả")
            {
                query = query.Where(item => item.LoaiCanhBao == loai);
            }

            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả")
            {
                query = query.Where(item => item.TrangThai == trangThai);
            }

            var items = query.OrderByDescending(item => item.NgayGio).Take(200).ToList()
                .Select(MapAlarm).ToList();

            return View(new AlarmFilterViewModel
            {
                MaMay = maMay ?? "Tất cả",
                LoaiCanhBao = loai ?? "Tất cả",
                TrangThai = trangThai ?? "Tất cả",
                Items = items,
                MachineOptions = new List<string> { "Tất cả" }.Concat(machineService.GetAll().Select(item => item.MaMay)).ToList(),
                ActiveCount = items.Count(item => item.TrangThai == "CHƯA XỬ LÝ")
            });
        }

        [Authorize(Roles = "Admin,Maintenance")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhanTatCa()
        {
            using var db = contextFactory.CreateDbContext();
            var now = DateTime.Now;
            foreach (var alarm in db.AlarmRecords.Where(item => item.TrangThai == "CHƯA XỬ LÝ"))
            {
                alarm.TrangThai = "ĐÃ XÁC NHẬN";
                alarm.NgayGioKetThuc ??= now;
                alarm.DurationSeconds = Math.Max(0, (int)(alarm.NgayGioKetThuc.Value - alarm.NgayGio).TotalSeconds);
            }

            db.SaveChanges();
            heThong.AddEvent("Xác nhận tất cả báo động");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Maintenance")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XacNhan(long id)
        {
            using var db = contextFactory.CreateDbContext();
            var alarm = db.AlarmRecords.FirstOrDefault(item => item.Id == id);
            if (alarm != null && alarm.TrangThai == "CHƯA XỬ LÝ")
            {
                alarm.TrangThai = "ĐÃ XÁC NHẬN";
                alarm.NgayGioKetThuc = DateTime.Now;
                alarm.DurationSeconds = Math.Max(0, (int)(alarm.NgayGioKetThuc.Value - alarm.NgayGio).TotalSeconds);
                db.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ExportExcel(string? maMay = null, string? loai = null, string? trangThai = null)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.AlarmRecords.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(maMay) && maMay != "Tất cả") query = query.Where(item => item.ThietBi == maMay);
            if (!string.IsNullOrWhiteSpace(loai) && loai != "Tất cả") query = query.Where(item => item.LoaiCanhBao == loai);
            if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả") query = query.Where(item => item.TrangThai == trangThai);
            var alarms = query.OrderByDescending(item => item.NgayGio).Take(5000).ToList();
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("CanhBao");
            var headers = new[] { "Mức độ", "Thiết bị", "Nội dung", "Loại", "Thời gian", "Trạng thái", "Xử lý lúc" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
            for (var row = 0; row < alarms.Count; row++)
            {
                var item = alarms[row];
                sheet.Cell(row + 2, 1).Value = item.MucDo;
                sheet.Cell(row + 2, 2).Value = item.ThietBi;
                sheet.Cell(row + 2, 3).Value = item.NoiDung;
                sheet.Cell(row + 2, 4).Value = item.LoaiCanhBao;
                sheet.Cell(row + 2, 5).Value = item.NgayGio;
                sheet.Cell(row + 2, 6).Value = item.TrangThai;
                sheet.Cell(row + 2, 7).Value = item.NgayGioKetThuc;
            }
            sheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"lich-su-canh-bao-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        [HttpGet]
        public IActionResult ActiveCount()
        {
            using var db = contextFactory.CreateDbContext();
            var count = db.AlarmRecords.Count(item => item.TrangThai == "CHƯA XỬ LÝ");
            return Json(new { count });
        }

        private static AlarmItem MapAlarm(AlarmRecord item) => new()
        {
            Id = item.Id,
            MucDo = item.MucDo,
            ThietBi = item.ThietBi,
            NoiDung = item.NoiDung,
            ThoiGian = item.NgayGio.ToString("dd/MM HH:mm"),
            NgayGio = item.NgayGio,
            NgayGioKetThuc = item.NgayGioKetThuc,
            DurationSeconds = item.DurationSeconds,
            TrangThai = item.TrangThai,
            LoaiCanhBao = item.LoaiCanhBao
        };
    }
}
