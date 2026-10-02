using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using Zentro.Services;
using Zentro.Models;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin,Maintenance")]
    public class BaoTriController : Controller
    {
        private readonly NhaMayDataService nhaMayData;
        private readonly MachineService machineService;
        private readonly IDbContextFactory<Zentro.Data.ProductionDbContext> contextFactory;

        public BaoTriController(NhaMayDataService nhaMayData, MachineService machineService, IDbContextFactory<Zentro.Data.ProductionDbContext> contextFactory)
        {
            this.nhaMayData = nhaMayData;
            this.machineService = machineService;
            this.contextFactory = contextFactory;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Bảo trì thiết bị";
            ViewData["Menu"] = "baotri";
            using (var db = contextFactory.CreateDbContext())
            {
                ViewBag.History = db.MaintenanceHistories.AsNoTracking().OrderByDescending(item => item.GhiNhanLuc).Take(200).ToList();
            }
            return View(nhaMayData.GetMaintenance());
        }

        [HttpGet]
        public IActionResult Create() => View("Edit", new MaintenanceEditViewModel { Machines = machineService.GetAll(activeOnly: true) });

        [HttpGet]
        public IActionResult Edit(long id)
        {
            using var db = contextFactory.CreateDbContext();
            var item = db.MaintenanceRecords.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();
            return View(new MaintenanceEditViewModel { Id = item.Id, ThietBi = item.ThietBi, Loai = item.Loai, NgayBt = item.NgayBt, LanTiepTheo = item.LanTiepTheo, TrangThai = item.TrangThai, NguoiThucHien = item.NguoiThucHien, GhiChu = item.GhiChu, Machines = machineService.GetAll(activeOnly: true) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(MaintenanceEditViewModel model)
        {
            model.Machines = machineService.GetAll(activeOnly: true);
            if (model.LanTiepTheo < model.NgayBt)
            {
                ModelState.AddModelError(nameof(model.LanTiepTheo), "Lần tiếp theo phải sau hoặc bằng ngày bảo trì.");
            }

            if (!ModelState.IsValid) return View(model);
            using var db = contextFactory.CreateDbContext();
            var item = model.Id == 0 ? new MaintenanceRecord() : db.MaintenanceRecords.FirstOrDefault(x => x.Id == model.Id);
            if (item == null) return NotFound();
            item.ThietBi = model.ThietBi;
            item.Loai = model.Loai;
            item.NgayBt = model.NgayBt;
            item.LanTiepTheo = model.LanTiepTheo;
            item.TrangThai = model.TrangThai;
            item.NguoiThucHien = model.NguoiThucHien;
            item.GhiChu = model.GhiChu;
            if (model.Id == 0) db.MaintenanceRecords.Add(item);
            db.SaveChanges();
            db.MaintenanceHistories.Add(new MaintenanceHistoryRecord
            {
                MaintenanceId = item.Id,
                ThietBi = item.ThietBi,
                Loai = item.Loai,
                NgayBt = item.NgayBt,
                LanTiepTheo = item.LanTiepTheo,
                TrangThai = item.TrangThai,
                NguoiThucHien = item.NguoiThucHien,
                GhiChu = item.GhiChu,
                GhiNhanLuc = DateTime.Now
            });
            db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            using var db = contextFactory.CreateDbContext();
            var item = db.MaintenanceRecords.FirstOrDefault(x => x.Id == id);
            if (item != null) { db.MaintenanceRecords.Remove(item); db.SaveChanges(); }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ExportExcel()
        {
            using var db = contextFactory.CreateDbContext();
            var records = db.MaintenanceRecords.AsNoTracking().OrderBy(item => item.LanTiepTheo).ToList();
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("BaoTri");
            var headers = new[] { "Thiết bị", "Loại", "Ngày bảo trì", "Lần tiếp theo", "Trạng thái", "Người thực hiện", "Ghi chú" };
            for (var column = 0; column < headers.Length; column++) sheet.Cell(1, column + 1).Value = headers[column];
            for (var row = 0; row < records.Count; row++)
            {
                var item = records[row];
                sheet.Cell(row + 2, 1).Value = item.ThietBi;
                sheet.Cell(row + 2, 2).Value = item.Loai;
                sheet.Cell(row + 2, 3).Value = item.NgayBt;
                sheet.Cell(row + 2, 4).Value = item.LanTiepTheo;
                sheet.Cell(row + 2, 5).Value = item.TrangThai;
                sheet.Cell(row + 2, 6).Value = item.NguoiThucHien;
                sheet.Cell(row + 2, 7).Value = item.GhiChu;
            }
            sheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"lich-su-bao-tri-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
    }
}
