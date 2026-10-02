using System.ComponentModel.DataAnnotations;

namespace Zentro.Models
{
    public class SensorItem
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "ACTIVE";
        public string? DonVi { get; set; }
        public string? MoTa { get; set; }
        public string GiaTri { get; set; } = string.Empty;
        public bool IsOn { get; set; }
    }

    public class AlarmItem
    {
        public long Id { get; set; }
        public string MucDo { get; set; } = "INFO";
        public string ThietBi { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
        public DateTime NgayGio { get; set; } = DateTime.Today;
        public DateTime? NgayGioKetThuc { get; set; }
        public int DurationSeconds { get; set; }
        public string ThoiLuong => DurationSeconds <= 0
            ? (NgayGioKetThuc.HasValue ? $"{(int)(NgayGioKetThuc.Value - NgayGio).TotalSeconds}s" : "Đang xử lý")
            : $"{DurationSeconds}s";
        public string TrangThai { get; set; } = "CHƯA XỬ LÝ";
        public string? LoaiCanhBao { get; set; }
    }

    public class MaintenanceItem
    {
        public long Id { get; set; }
        public string ThietBi { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string NgayBt { get; set; } = string.Empty;
        public string LanTiepTheo { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "OK";
        public string NguoiThucHien { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public bool SapDenHan { get; set; }
        public bool QuaHan { get; set; }
    }

    public class ProductHistoryItem
    {
        public string MaSp { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string KetQua { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty;
        public DateTime NgayGio { get; set; } = DateTime.Now;
        public string Ca { get; set; } = "CA 1";
        public string Lo { get; set; } = string.Empty;
        public double CycleTimeSeconds { get; set; }
    }

    public class SystemConfig
    {
        [Range(0, 100)]
        public int TocDoMacDinh { get; set; } = 50;
        [Range(1, 100)]
        public int TocDoToiDa { get; set; } = 100;
        [Range(0, 10)]
        public int DelayCamBien { get; set; } = 2;
        [Range(1, 3600)]
        public int ChuKyLuuSensor { get; set; } = 10;
        [Range(0, 3600)]
        public int ThoiGianCanhBao { get; set; }
        public bool TuDongKhoiDong { get; set; } = true;
        public bool ChoPhepChay { get; set; } = true;
    }

    public class HmiDashboardViewModel
    {
        public BangTaiModel HeThong { get; set; } = new();
        public List<SensorItem> CamBiens { get; set; } = new();
        public List<AlarmItem> BaoDongs { get; set; } = new();
        public string TrangThaiNhaMay { get; set; } = "HỆ THỐNG ĐANG HOẠT ĐỘNG";
    }

    // Dashboard Monitoring Models
    public class DashboardProductionSummary
    {
        public int TongSanLuong { get; set; }
        public int SanLuongOK { get; set; }
        public int SanLuongNG { get; set; }
        public decimal TyLeOK => TongSanLuong > 0 ? (decimal)SanLuongOK * 100 / TongSanLuong : 0;
        public string SanPhamHienTai { get; set; } = string.Empty;
        public string LoHienTai { get; set; } = string.Empty;
        public TimeSpan ThoiGianChay { get; set; }
        public TimeSpan ThoiGianDung { get; set; }
        public int SoLanCanhBao { get; set; }
    }

    public class DashboardDeviceStatus
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "STOPPED";
        public string Loai { get; set; } = string.Empty;
        public int TocDo { get; set; }
        public int TocDoPhanTram => TocDo;
        public string UrlChiTiet { get; set; } = string.Empty;
    }

    public class DashboardSensorStatus
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string GiaTri { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string DonVi { get; set; } = string.Empty;
    }

    public class DashboardPlcStatus
    {
        public bool IsConnected { get; set; }
        public DateTime? LastCommunication { get; set; }
        public long? SecondsSinceLastUpdate { get; set; }
        public bool IsStale { get; set; }
    }

    public class DashboardMonitoringModel
    {
        public DashboardProductionSummary SanXuat { get; set; } = new();
        public DashboardPlcStatus Plc { get; set; } = new();
        public List<DashboardDeviceStatus> ThietBi { get; set; } = new();
        public List<DashboardSensorStatus> CamBien { get; set; } = new();
        public List<AlarmItem> BaoDong { get; set; } = new();
        public List<MaintenanceItem> BaoTri { get; set; } = new();
        
        // System status
        public string TrangThaiHeThong { get; set; } = "NORMAL";
        public int SoThietBiHoatDong { get; set; }
        public int TongSoMay { get; set; }
        public int SoMayRunning { get; set; }
        public int SoMayPaused { get; set; }
        public int SoMayStopped { get; set; }
        public int SoMayAlarm { get; set; }
        public double TyLeHoatDong { get; set; }
        public int SoCamBienHoatDong { get; set; }
        public int SoBaoDongChuaXuLy { get; set; }
        public DateTime ThoiGianCapNhat { get; set; } = DateTime.Now;
    }

    public class DeviceDetailViewModel
    {
        public DashboardDeviceStatus ThietBi { get; set; } = new();
        public DashboardPlcStatus Plc { get; set; } = new();
        public List<DashboardSensorStatus> CamBien { get; set; } = new();
        public int SanLuong { get; set; }
        public List<EventLogItem> LichSu { get; set; } = new();
    }

    public class OperationLogViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
        public List<EventLogItem> Items { get; set; } = new();
        public List<UserActionLogRecord> ActionItems { get; set; } = new();
    }
}
