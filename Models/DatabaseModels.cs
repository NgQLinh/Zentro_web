namespace Zentro.Models
{
    public class ProductionLotRecord
    {
        public long Id { get; set; }
        public string Lot { get; set; } = string.Empty;
        public string MaSp { get; set; } = string.Empty;
        public int SoLuongKeHoach { get; set; }
        public int SoLuongThucTe { get; set; }
        public int SoLuongOK { get; set; }
        public int SoLuongNG { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string TrangThai { get; set; } = "WAITING";
        public string? GhiChu { get; set; }
    }

    public class DeviceRecord
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "STOPPED";
        public string? ViTri { get; set; }
        public string? MoTa { get; set; }
    }

    public class MachineSessionRecord
    {
        public long Id { get; set; }
        public string? Lot { get; set; }
        public string CheDo { get; set; } = string.Empty;
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public int TongCycle { get; set; }
        public int SoLuongOK { get; set; }
        public int SoLuongNG { get; set; }
        public string TrangThai { get; set; } = "RUNNING";
    }

    public class SensorDataRecord
    {
        public long Id { get; set; }
        public string MaSensor { get; set; } = string.Empty;
        public double GiaTri { get; set; }
        public DateTime NgayGio { get; set; }
    }

    public class ProductionRecord
    {
        public long Id { get; set; }
        public string ThietBi { get; set; } = "BT01";
        public long? SessionId { get; set; }
        public string MaSp { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string KetQua { get; set; } = string.Empty;
        public DateTime NgayGio { get; set; }
        public DateTime? NgayGioKetThuc { get; set; }
        public string Ca { get; set; } = string.Empty;
        public string Lot { get; set; } = string.Empty;
        public double CycleTimeSeconds { get; set; }
    }

    public class DeviceConfigRecord
    {
        public string ThietBi { get; set; } = string.Empty;
        public int TocDoMacDinh { get; set; }
        public int TocDoToiDa { get; set; }
        public int DelayCamBien { get; set; }
        public bool TuDongKhoiDong { get; set; }
        public bool ChoPhepChay { get; set; }
        public int ChuKyLuuSensor { get; set; }
        public int ThoiGianCanhBao { get; set; }
    }

    public class AlarmRecord
    {
        public long Id { get; set; }
        public string MucDo { get; set; } = "INFO";
        public string ThietBi { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public DateTime NgayGio { get; set; }
        public DateTime? NgayGioKetThuc { get; set; }
        public int DurationSeconds { get; set; }
        public string TrangThai { get; set; } = "CHƯA XỬ LÝ";
        public string? LoaiCanhBao { get; set; }
    }

    public class EventRecord
    {
        public long Id { get; set; }
        public DateTime NgayGio { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string? LoaiSuKien { get; set; }
        public string? ThietBi { get; set; }
        public string? CheDo { get; set; }
    }

    public class SensorRecord
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "ACTIVE";
        public string? DonVi { get; set; }
        public string? MoTa { get; set; }
    }

    public class MaintenanceRecord
    {
        public long Id { get; set; }
        public string ThietBi { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public DateTime NgayBt { get; set; }
        public DateTime LanTiepTheo { get; set; }
        public string TrangThai { get; set; } = "OK";
        public string NguoiThucHien { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }

    public class MaintenanceHistoryRecord
    {
        public long Id { get; set; }
        public long MaintenanceId { get; set; }
        public string ThietBi { get; set; } = string.Empty;
        public string Loai { get; set; } = string.Empty;
        public DateTime NgayBt { get; set; }
        public DateTime LanTiepTheo { get; set; }
        public string TrangThai { get; set; } = "OK";
        public string NguoiThucHien { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public DateTime GhiNhanLuc { get; set; } = DateTime.Now;
    }

    public class SystemConfigRecord
    {
        public int Id { get; set; }
        public int TocDoMacDinh { get; set; }
        public int TocDoToiDa { get; set; }
        public int DelayCamBien { get; set; }
        public bool TuDongKhoiDong { get; set; }
        public bool ChoPhepChay { get; set; }
        public int ChuKyLuuSensor { get; set; }
        public int ThoiGianCanhBao { get; set; }
    }

    public class MachineRecord
    {
        public string MaMay { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string? IpPlc { get; set; }
        public int PortPlc { get; set; } = 502;
        public string TrangThai { get; set; } = "STOPPED";
        public bool IsActive { get; set; } = true;
        public string? BangTaiMap { get; set; }
        public DateTime? TrangThaiCapNhatLuc { get; set; }
        public bool PlcOnline { get; set; }
    }

    public class MachineStatusHistoryRecord
    {
        public long Id { get; set; }
        public string MaMay { get; set; } = string.Empty;
        public string? TrangThaiCu { get; set; }
        public string TrangThaiMoi { get; set; } = string.Empty;
        public DateTime ThoiDiem { get; set; }
        public DateTime? ThoiDiemKetThuc { get; set; }
        public int? ThoiLuongGiay { get; set; }
        public string Nguon { get; set; } = "SYSTEM";
        public string? LyDoDung { get; set; }
        public string? NguoiThucHien { get; set; }
        public string? GhiChu { get; set; }
    }

    public class PartRecord
    {
        public long Id { get; set; }
        public string MaIndex { get; set; } = string.Empty;
        public string MaSp { get; set; } = string.Empty;
        public string TenChiTiet { get; set; } = string.Empty;
        public string? MaMay { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class MachiningRecord
    {
        public long Id { get; set; }
        public string MaMay { get; set; } = string.Empty;
        public long PartId { get; set; }
        public string MaSp { get; set; } = string.Empty;
        public string MaIndex { get; set; } = string.Empty;
        public string TenChiTiet { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string? Username { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public string TrangThai { get; set; } = "RUNNING";
        public string? KetQua { get; set; }
        public string? Lot { get; set; }
        public long? ProductionRecordId { get; set; }
        public double CycleTimeSeconds { get; set; }
    }

    public class PlcDataRecord
    {
        public long Id { get; set; }
        public string MaMay { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double GiaTri { get; set; }
        public DateTime NgayGio { get; set; }
    }

    public class UserRecord
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class UserMachineAssignmentRecord
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string MaMay { get; set; } = string.Empty;
    }

    public class AuthenticationSettingsRecord
    {
        public int Id { get; set; }
        public string AdminPin { get; set; } = string.Empty;
        public string UserPin { get; set; } = string.Empty;
    }

    public class UserActionLogRecord
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime ThoiDiem { get; set; }
        public string LoaiThaoTac { get; set; } = string.Empty;
        public string? MaMay { get; set; }
        public string? MaIndex { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public bool ThanhCong { get; set; }
        public string? Loi { get; set; }
    }
}
