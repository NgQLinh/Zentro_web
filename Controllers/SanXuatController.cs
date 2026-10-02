using System.Net;
using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;
using Zentro.Services;

namespace Zentro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SanXuatController : Controller
    {
        private readonly BangTaiModel heThong;
        private readonly NhaMayDataService nhaMayData;
        private readonly ProductionDatabaseService database;
        private readonly MachineService machineService;
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;

        public SanXuatController(
            BangTaiModel heThong,
            NhaMayDataService nhaMayData,
            ProductionDatabaseService database,
            MachineService machineService,
            IDbContextFactory<ProductionDbContext> contextFactory)
        {
            this.heThong = heThong;
            this.nhaMayData = nhaMayData;
            this.database = database;
            this.machineService = machineService;
            this.contextFactory = contextFactory;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý sản xuất";
            ViewData["Menu"] = "sanxuat";
            return View(heThong);
        }

        public IActionResult LoSanXuat()
        {
            ViewData["Title"] = "Lô sản xuất";
            ViewData["Menu"] = "losanxuat";
            return View(heThong);
        }

        public IActionResult SanLuong()
        {
            ViewData["Title"] = "Sản lượng";
            ViewData["Menu"] = "sanluong";
            return View(heThong);
        }

        public IActionResult ThongKe()
        {
            ViewData["Title"] = "Báo cáo / Thống kê";
            ViewData["Menu"] = "thongke";
            var storedRows = database.GetProduction(DateTime.Today.AddYears(-1), DateTime.Today.AddDays(1));
            var total = storedRows.Count > 0 ? storedRows.Count : heThong.SoSanPham;
            var ok = storedRows.Count > 0 ? storedRows.Count(item => item.KetQua != "Lỗi") : heThong.TongSanPhamDat;
            var productCounts = storedRows.Count > 0
                ? storedRows.GroupBy(item => item.Loai).ToDictionary(group => group.Key, group => group.Count())
                : new Dictionary<string, int>(heThong.SoLuongTheoLoai);
            var trend = (storedRows.Count > 0 ? storedRows.Select(item => item.NgayGio) : heThong.ProductHistory.Select(item => item.NgayGio))
                .GroupBy(item => item.ToString("HH:mm"))
                .OrderBy(item => item.Key)
                .Select(item => new StatisticsPoint { Label = item.Key, Value = item.Count() })
                .TakeLast(12)
                .ToList();
            var oeeRows = storedRows.Where(item => item.ThietBi == "BT01").ToList();
            if (oeeRows.Count == 0)
            {
                oeeRows = heThong.ProductHistory.Select(item => new ProductionRecord
                {
                    ThietBi = "BT01",
                    KetQua = item.KetQua,
                    NgayGio = item.NgayGio,
                    Lot = item.Lo,
                    CycleTimeSeconds = item.CycleTimeSeconds
                }).ToList();
            }
            var oee = CalculateOee(oeeRows);
            return View(new StatisticsViewModel
            {
                HeThong = heThong,
                Total = total,
                Ok = ok,
                Ng = total - ok,
                ProductCounts = productCounts,
                AlarmCount = nhaMayData.GetAlarms().Count,
                Trend = trend
                ,Oee = oee
                ,OeeByHour = BuildOeePoints(oeeRows, item => item.NgayGio.ToString("HH:00"))
                ,OeeByDay = BuildOeePoints(oeeRows, item => item.NgayGio.ToString("dd/MM"))
                ,OeeByLot = BuildOeePoints(oeeRows, item => string.IsNullOrWhiteSpace(item.Lot) ? "Không có lot" : item.Lot)
            });
        }

        private OeeMetrics CalculateOee(IReadOnlyList<ProductionRecord> rows)
        {
            var totalWindow = heThong.RunTime + heThong.StopTime;
            var availability = totalWindow == TimeSpan.Zero ? 0 : heThong.RunTime.TotalSeconds / totalWindow.TotalSeconds;
            var averageCycle = rows.Where(item => item.CycleTimeSeconds > 0).Select(item => item.CycleTimeSeconds).DefaultIfEmpty().Average();
            var standardCycle = Math.Max(0.1, nhaMayData.GetConfig().DelayCamBien);
            var performance = averageCycle <= 0 ? 0 : Math.Min(1, standardCycle / averageCycle);
            var quality = rows.Count == 0 ? 0 : rows.Count(item => item.KetQua != "Lỗi") / (double)rows.Count;
            return new OeeMetrics { Availability = availability, Performance = performance, Quality = quality };
        }

        private List<OeePoint> BuildOeePoints(IEnumerable<ProductionRecord> rows, Func<ProductionRecord, string> labelSelector)
        {
            return rows.GroupBy(labelSelector)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var total = group.Count();
                    var quality = total == 0 ? 0 : group.Count(item => item.KetQua != "Lỗi") / (double)total;
                    var averageCycle = group.Where(item => item.CycleTimeSeconds > 0).Select(item => item.CycleTimeSeconds).DefaultIfEmpty().Average();
                    var performance = averageCycle <= 0 ? 0 : Math.Min(1, nhaMayData.GetConfig().DelayCamBien / averageCycle);
                    return new OeePoint { Label = group.Key, Oee = quality * performance * 100 };
                })
                .ToList();
        }

        [HttpGet]
        public IActionResult BaoCao(ReportFilterViewModel? filter = null)
        {
            ViewData["Title"] = "Báo cáo sản xuất";
            ViewData["Menu"] = "baocao";
            return View(BuildReport(filter));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Kpi(string period = "day", DateTime? from = null, DateTime? to = null, string? maMay = null, string? maSp = null)
        {
            ViewData["Title"] = "Báo cáo KPI";
            ViewData["Menu"] = "kpi";
            return View(BuildKpi(period, from, to, maMay, maSp));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult ExportKpiExcel(string period = "day", DateTime? from = null, DateTime? to = null, string? maMay = null, string? maSp = null)
        {
            var report = BuildKpi(period, from, to, maMay, maSp);
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("KPI");
            sheet.Cell(1, 1).Value = "BÁO CÁO KPI SẢN XUẤT";
            sheet.Cell(2, 1).Value = $"Kỳ: {report.Period} | Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}";
            sheet.Cell(3, 1).Value = $"Máy: {report.MaMay} | Mã SP: {report.MaSp}";
            sheet.Cell(5, 1).Value = "Tổng sản lượng";
            sheet.Cell(5, 2).Value = report.TongSanLuong;
            sheet.Cell(6, 1).Value = "Số lần gia công";
            sheet.Cell(6, 2).Value = report.SoLanGiaCong;
            sheet.Cell(7, 1).Value = "Sản phẩm đạt";
            sheet.Cell(7, 2).Value = report.SanPhamDat;
            sheet.Cell(8, 1).Value = "Sản phẩm lỗi";
            sheet.Cell(8, 2).Value = report.SanPhamLoi;
            sheet.Cell(9, 1).Value = "Thời gian hoạt động (giây)";
            sheet.Cell(9, 2).Value = report.ThoiGianHoatDong.TotalSeconds;
            sheet.Cell(10, 1).Value = "Số sự cố";
            sheet.Cell(10, 2).Value = report.SoSuCo;
            sheet.Cell(11, 1).Value = "Hiệu suất %";
            sheet.Cell(11, 2).Value = report.HieuSuatPercent;

            var detail = workbook.Worksheets.Add("GiaCong");
            detail.Cell(1, 1).Value = "Máy";
            detail.Cell(1, 2).Value = "Index";
            detail.Cell(1, 3).Value = "Mã SP";
            detail.Cell(1, 4).Value = "Chi tiết";
            detail.Cell(1, 5).Value = "Nhân viên";
            detail.Cell(1, 6).Value = "Bắt đầu";
            detail.Cell(1, 7).Value = "Kết thúc";
            detail.Cell(1, 8).Value = "Kết quả";
            detail.Cell(1, 9).Value = "Cycle(s)";
            var row = 2;
            foreach (var item in report.MachiningRows)
            {
                detail.Cell(row, 1).Value = item.MaMay;
                detail.Cell(row, 2).Value = item.MaIndex;
                detail.Cell(row, 3).Value = item.MaSp;
                detail.Cell(row, 4).Value = item.TenChiTiet;
                detail.Cell(row, 5).Value = item.Username;
                detail.Cell(row, 6).Value = item.ThoiGianBatDau;
                detail.Cell(row, 7).Value = item.ThoiGianKetThuc;
                detail.Cell(row, 8).Value = item.KetQua;
                detail.Cell(row, 9).Value = item.CycleTimeSeconds;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BaoCaoKPI.xlsx");
        }

        private KpiReportViewModel BuildKpi(string period, DateTime? from, DateTime? to, string? maMay, string? maSp)
        {
            var today = DateTime.Today;
            DateTime fromDate;
            DateTime toDate;
            switch ((period ?? "day").ToLowerInvariant())
            {
                case "week":
                    var diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7;
                    fromDate = today.AddDays(-diff);
                    toDate = fromDate.AddDays(6);
                    period = "week";
                    break;
                case "month":
                    fromDate = new DateTime(today.Year, today.Month, 1);
                    toDate = fromDate.AddMonths(1).AddDays(-1);
                    period = "month";
                    break;
                default:
                    fromDate = from?.Date ?? today;
                    toDate = to?.Date ?? fromDate;
                    period = "day";
                    break;
            }

            if (toDate < fromDate)
            {
                toDate = fromDate;
            }

            var toExclusive = toDate.Date.AddDays(1);
            maMay ??= "Tất cả";
            maSp ??= "Tất cả";

            using var db = contextFactory.CreateDbContext();
            var machiningQuery = db.MachiningRecords.AsNoTracking()
                .Where(item => item.ThoiGianBatDau >= fromDate && item.ThoiGianBatDau < toExclusive);
            if (maMay != "Tất cả")
            {
                machiningQuery = machiningQuery.Where(item => item.MaMay == maMay);
            }

            if (maSp != "Tất cả")
            {
                machiningQuery = machiningQuery.Where(item => item.MaSp == maSp);
            }

            var machiningRows = machiningQuery.OrderByDescending(item => item.ThoiGianBatDau).ToList();
            var production = database.GetProduction(fromDate, toExclusive)
                .Where(item => maMay == "Tất cả" || item.ThietBi == maMay)
                .Where(item => maSp == "Tất cả" || item.MaSp == maSp)
                .ToList();
            var alarms = database.GetAlarms(fromDate, toExclusive)
                .Where(item => maMay == "Tất cả" || item.ThietBi == maMay)
                .ToList();
            var runtime = machineService.GetRunningTime(maMay, fromDate, toExclusive);
            var windowSeconds = (toExclusive - fromDate).TotalSeconds;
            var ok = machiningRows.Count(item => item.KetQua == "OK") + production.Count(item => item.KetQua != "Lỗi" && item.KetQua != "PROCESS");
            var ng = machiningRows.Count(item => item.KetQua is "NG" or "Lỗi") + production.Count(item => item.KetQua == "Lỗi");
            var total = Math.Max(machiningRows.Count(item => item.TrangThai == "COMPLETED"), production.Count(item => item.KetQua != "PROCESS"));

            return new KpiReportViewModel
            {
                Period = period,
                FromDate = fromDate,
                ToDate = toDate,
                MaMay = maMay,
                MaSp = maSp,
                MachineOptions = new List<string> { "Tất cả" }.Concat(machineService.GetAll().Select(item => item.MaMay)).ToList(),
                ProductOptions = new List<string> { "Tất cả" }
                    .Concat(db.Parts.AsNoTracking().Select(item => item.MaSp).Distinct().OrderBy(item => item))
                    .ToList(),
                TongSanLuong = total,
                SoLanGiaCong = machiningRows.Count,
                SanPhamDat = ok,
                SanPhamLoi = ng,
                SoSuCo = alarms.Count,
                ThoiGianHoatDong = runtime,
                HieuSuatPercent = windowSeconds <= 0 ? 0 : Math.Round(runtime.TotalSeconds * 100 / windowSeconds, 1),
                MachiningRows = machiningRows.Take(200).ToList(),
                Alarms = alarms.Select(item => new AlarmItem
                {
                    Id = item.Id,
                    MucDo = item.MucDo,
                    ThietBi = item.ThietBi,
                    NoiDung = item.NoiDung,
                    NgayGio = item.NgayGio,
                    NgayGioKetThuc = item.NgayGioKetThuc,
                    ThoiGian = item.NgayGio.ToString("dd/MM HH:mm"),
                    DurationSeconds = item.DurationSeconds,
                    TrangThai = item.TrangThai,
                    LoaiCanhBao = item.LoaiCanhBao
                }).ToList()
            };
        }

        [HttpGet]
        public IActionResult ExportExcel(ReportFilterViewModel? filter = null)
        {
            var report = BuildReport(filter, paginate: false);
            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'></head><body>");
            html.Append("<h1>BÁO CÁO SẢN XUẤT</h1>");
            html.Append($"<p>Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy} | Thiết bị: {WebUtility.HtmlEncode(report.Device)} | Ca: {WebUtility.HtmlEncode(report.Ca)} | Lot: {WebUtility.HtmlEncode(report.Lot)} | Sản phẩm: {WebUtility.HtmlEncode(report.Product)}</p>");
            html.Append($"<p>Total: {report.Summary.Total} | OK: {report.Summary.Ok} | NG: {report.Summary.Ng} | Yield: {report.Summary.YieldPercent}%</p>");
            if (report.LotSummary != null)
            {
                html.Append($"<h2>LOT {WebUtility.HtmlEncode(report.LotSummary.Lot)}</h2>");
                html.Append($"<p>Mã SP: {WebUtility.HtmlEncode(report.LotSummary.MaSp)} | Kế hoạch: {report.LotSummary.KeHoach} | Đã SX: {report.LotSummary.DaSanXuat} | OK: {report.LotSummary.Ok} | NG: {report.LotSummary.Ng} | Tỷ lệ đạt: {report.LotSummary.TyLeDat:F2}%</p>");
                html.Append($"<p>Bắt đầu: {report.LotSummary.BatDau:dd/MM/yyyy HH:mm:ss} | Kết thúc: {report.LotSummary.KetThuc:dd/MM/yyyy HH:mm:ss}</p>");
                html.Append("<p>Sản lượng theo line: ");
                html.Append(string.Join(" | ", report.LotSummary.SanLuongTheoLine.Select(item => $"{WebUtility.HtmlEncode(item.Key)}: {item.Value}")));
                html.Append("</p>");
            }
            html.Append("<table border='1'><tr><th>Thiết bị</th><th>Mã SP</th><th>Loại</th><th>Kết quả</th><th>Cycle Time</th><th>Ngày giờ</th><th>Ca</th><th>Lot</th></tr>");
            foreach (var row in report.ProductionRows)
            {
                html.Append($"<tr><td>{WebUtility.HtmlEncode(row.ThietBi)}</td><td>{WebUtility.HtmlEncode(row.MaSp)}</td><td>{WebUtility.HtmlEncode(row.Loai)}</td><td>{WebUtility.HtmlEncode(row.KetQua)}</td><td>{row.CycleTimeSeconds:0.0}s</td><td>{row.NgayGio:dd/MM/yyyy HH:mm:ss}</td><td>{WebUtility.HtmlEncode(row.Ca)}</td><td>{WebUtility.HtmlEncode(row.Lot)}</td></tr>");
            }
            html.Append("</table><h2>ALARM</h2><table border='1'><tr><th>Mức độ</th><th>Thiết bị</th><th>Nội dung</th><th>Thời gian</th><th>Thời lượng</th></tr>");
            foreach (var alarm in report.Alarms)
            {
                html.Append($"<tr><td>{WebUtility.HtmlEncode(alarm.MucDo)}</td><td>{WebUtility.HtmlEncode(alarm.ThietBi)}</td><td>{WebUtility.HtmlEncode(alarm.NoiDung)}</td><td>{WebUtility.HtmlEncode(alarm.ThoiGian)}</td><td>{WebUtility.HtmlEncode(alarm.ThoiLuong)}</td></tr>");
            }
            html.Append("</table></body></html>");
            return File(Encoding.UTF8.GetBytes(html.ToString()), "application/vnd.ms-excel", "BaoCaoSanXuat.xls");
        }

        private ReportFilterViewModel BuildReport(ReportFilterViewModel? source, bool paginate = true)
        {
            var filter = source ?? new ReportFilterViewModel();
            if (filter.FromDate == default) filter.FromDate = DateTime.Today;
            if (filter.ToDate == default) filter.ToDate = filter.FromDate;
            if (filter.ToDate < filter.FromDate) filter.ToDate = filter.FromDate;
            var toExclusive = filter.ToDate.Date.AddDays(1);

            var allRows = database.GetProduction(filter.FromDate.Date, toExclusive)
                .Select(item => new ReportProductionRow
                {
                    ThietBi = item.ThietBi,
                    MaSp = item.MaSp,
                    Loai = item.Loai,
                    KetQua = item.KetQua,
                    NgayGio = item.NgayGio,
                    Ca = item.Ca,
                    Lot = item.Lot,
                    CycleTimeSeconds = item.CycleTimeSeconds
                }).ToList();

            allRows = allRows.Where(item => filter.Device == "Tất cả" || item.ThietBi == filter.Device).ToList();

            if (allRows.Count == 0 && filter.FromDate.Date <= DateTime.Today && filter.ToDate.Date >= DateTime.Today)
            {
                allRows = heThong.ProductHistory.Select(item => new ReportProductionRow
                {
                    ThietBi = "BT01",
                    MaSp = item.MaSp,
                    Loai = item.Loai,
                    KetQua = item.KetQua,
                    NgayGio = item.NgayGio,
                    Ca = item.Ca,
                    Lot = item.Lo,
                    CycleTimeSeconds = item.CycleTimeSeconds
                }).ToList();
            }

            filter.LotOptions = allRows.Select(item => item.Lot).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct().OrderBy(item => item).ToList();
            filter.ProductOptions = allRows.Select(item => item.Loai).Distinct().OrderBy(item => item).ToList();
            var matchingProductionRows = allRows.Where(item => item.NgayGio >= filter.FromDate.Date && item.NgayGio < toExclusive)
                .Where(item => filter.Ca == "Tất cả" || item.Ca == filter.Ca)
                .Where(item => filter.Lot == "Tất cả" || item.Lot == filter.Lot)
                .Where(item => filter.Product == "Tất cả" || item.Loai == filter.Product)
                .OrderByDescending(item => item.NgayGio)
                .ToList();
            filter.ProductionByDevice = matchingProductionRows
                .GroupBy(item => item.ThietBi)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.Count());
            filter.ProductionTotalRows = matchingProductionRows.Count;
            filter.ProductionPage = paginate
                ? Math.Clamp(filter.ProductionPage, 1, filter.ProductionTotalPages)
                : 1;
            filter.ProductionRows = paginate
                ? matchingProductionRows.Skip((filter.ProductionPage - 1) * filter.ProductionPageSize).Take(filter.ProductionPageSize).ToList()
                : matchingProductionRows;

            if (filter.Lot != "Tất cả")
            {
                var lot = database.GetLot(filter.Lot);
                if (lot != null)
                {
                    var lotFrom = lot.NgayBatDau?.Date ?? DateTime.Today.AddYears(-1);
                    var lotTo = (lot.NgayKetThuc?.Date ?? DateTime.Today).AddDays(1);
                    var lotRows = database.GetProduction(lotFrom, lotTo).Where(item => item.Lot == lot.Lot).ToList();
                    filter.LotSummary = new ReportLotSummary
                    {
                        Lot = lot.Lot,
                        MaSp = lot.MaSp,
                        KeHoach = lot.SoLuongKeHoach,
                        DaSanXuat = lot.SoLuongThucTe,
                        Ok = lot.SoLuongOK,
                        Ng = lot.SoLuongNG,
                        BatDau = lot.NgayBatDau,
                        KetThuc = lot.NgayKetThuc,
                        SanLuongTheoLine = lotRows.GroupBy(item => item.ThietBi).ToDictionary(group => group.Key, group => group.Count())
                    };
                }
            }

            var dbAlarms = database.GetAlarms(filter.FromDate.Date, toExclusive)
                .Select(item => new AlarmItem
                {
                    MucDo = item.MucDo,
                    ThietBi = item.ThietBi,
                    NoiDung = item.NoiDung,
                    NgayGio = item.NgayGio,
                    ThoiGian = item.NgayGio.ToString("HH:mm"),
                    DurationSeconds = item.DurationSeconds,
                    TrangThai = item.TrangThai
                });
            var allAlarms = dbAlarms.ToList();
            filter.AlarmTotalRows = allAlarms.Count;
            filter.AlarmPage = paginate
                ? Math.Clamp(filter.AlarmPage, 1, filter.AlarmTotalPages)
                : 1;
            filter.Alarms = paginate
                ? allAlarms.Skip((filter.AlarmPage - 1) * filter.AlarmPageSize).Take(filter.AlarmPageSize).ToList()
                : allAlarms;
            filter.Summary = new ReportSummary
            {
                Total = matchingProductionRows.Count,
                Ok = matchingProductionRows.Count(item => item.KetQua != "Lỗi"),
                Ng = matchingProductionRows.Count(item => item.KetQua == "Lỗi"),
                CycleTimeSeconds = matchingProductionRows.Count == 0 ? heThong.AverageCycleTimeSeconds : matchingProductionRows.Average(item => item.CycleTimeSeconds),
                RunTime = heThong.RunTime,
                StopTime = heThong.StopTime,
                AlarmCount = allAlarms.Count
            };
            return filter;
        }
    }
}
