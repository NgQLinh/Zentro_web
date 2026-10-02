namespace Zentro.Models
{
    public class ReportFilterViewModel
    {
        public DateTime FromDate { get; set; } = DateTime.Today;
        public DateTime ToDate { get; set; } = DateTime.Today;
        public string Ca { get; set; } = "Tất cả";
        public string Lot { get; set; } = "Tất cả";
        public string Product { get; set; } = "Tất cả";
        public string Device { get; set; } = "Tất cả";
        public int ProductionPage { get; set; } = 1;
        public int ProductionPageSize { get; set; } = 20;
        public int ProductionTotalRows { get; set; }
        public int ProductionTotalPages => Math.Max(1, (ProductionTotalRows + ProductionPageSize - 1) / ProductionPageSize);
        public int AlarmPage { get; set; } = 1;
        public int AlarmPageSize { get; set; } = 20;
        public int AlarmTotalRows { get; set; }
        public int AlarmTotalPages => Math.Max(1, (AlarmTotalRows + AlarmPageSize - 1) / AlarmPageSize);
        public List<string> CaOptions { get; set; } = new() { "Tất cả", "CA 1", "CA 2", "CA 3" };
        public List<string> LotOptions { get; set; } = new();
        public List<string> ProductOptions { get; set; } = new();
        public List<string> DeviceOptions { get; set; } = new() { "Tất cả", "BT01", "BT02" };
        public ReportSummary Summary { get; set; } = new();
        public ReportLotSummary? LotSummary { get; set; }
        public Dictionary<string, int> ProductionByDevice { get; set; } = new();
        public List<ReportProductionRow> ProductionRows { get; set; } = new();
        public List<AlarmItem> Alarms { get; set; } = new();
    }

    public class ReportLotSummary
    {
        public string Lot { get; set; } = string.Empty;
        public string MaSp { get; set; } = string.Empty;
        public int KeHoach { get; set; }
        public int DaSanXuat { get; set; }
        public int Ok { get; set; }
        public int Ng { get; set; }
        public DateTime? BatDau { get; set; }
        public DateTime? KetThuc { get; set; }
        public Dictionary<string, int> SanLuongTheoLine { get; set; } = new();
        public decimal TyLeDat => DaSanXuat == 0 ? 0 : Ok * 100m / DaSanXuat;
    }

    public class ReportSummary
    {
        public int Total { get; set; }
        public int Ok { get; set; }
        public int Ng { get; set; }
        public int YieldPercent => Total == 0 ? 0 : Ok * 100 / Total;
        public double CycleTimeSeconds { get; set; }
        public TimeSpan RunTime { get; set; }
        public TimeSpan StopTime { get; set; }
        public int DowntimePercent => RunTime + StopTime == TimeSpan.Zero ? 0 : (int)(StopTime.TotalSeconds * 100 / (RunTime + StopTime).TotalSeconds);
        public int AlarmCount { get; set; }
    }

    public class ReportProductionRow
    {
        public string ThietBi { get; set; } = "BT01";
        public string MaSp { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string KetQua { get; set; } = string.Empty;
        public DateTime NgayGio { get; set; }
        public string Ca { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public double CycleTimeSeconds { get; set; }
    }

    public class StatisticsViewModel
    {
        public BangTaiModel HeThong { get; set; } = new();
        public int Total { get; set; }
        public int Ok { get; set; }
        public int Ng { get; set; }
        public int YieldPercent => Total == 0 ? 0 : Ok * 100 / Total;
        public Dictionary<string, int> ProductCounts { get; set; } = new();
        public int AlarmCount { get; set; }
        public List<StatisticsPoint> Trend { get; set; } = new();
        public OeeMetrics Oee { get; set; } = new();
        public List<OeePoint> OeeByHour { get; set; } = new();
        public List<OeePoint> OeeByDay { get; set; } = new();
        public List<OeePoint> OeeByLot { get; set; } = new();
    }

    public class OeeMetrics
    {
        public double Availability { get; set; }
        public double Performance { get; set; }
        public double Quality { get; set; }
        public double Oee => Availability * Performance * Quality;
    }

    public class OeePoint
    {
        public string Label { get; set; } = string.Empty;
        public double Oee { get; set; }
    }

    public class StatisticsPoint
    {
        public string Label { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
