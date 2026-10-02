using System.ComponentModel.DataAnnotations;

namespace Zentro.Models
{
    public class MachineEditViewModel
    {
        [Required, StringLength(50)]
        public string MaMay { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string Ten { get; set; } = string.Empty;
        [StringLength(50)]
        public string? IpPlc { get; set; }
        [Range(1, 65535)]
        public int PortPlc { get; set; } = 502;
        public bool IsActive { get; set; } = true;
        public string? BangTaiMap { get; set; }
        public string TrangThai { get; set; } = "STOPPED";
        public bool IsNew { get; set; } = true;
    }

    public class PartEditViewModel
    {
        public long Id { get; set; }
        [Required, StringLength(50)]
        public string MaIndex { get; set; } = string.Empty;
        [Required, StringLength(50)]
        public string MaSp { get; set; } = string.Empty;
        [Required, StringLength(150)]
        public string TenChiTiet { get; set; } = string.Empty;
        public string? MaMay { get; set; }
        public List<MachineRecord> Machines { get; set; } = new();
    }

    public class GiaCongViewModel
    {
        public List<MachineRecord> Machines { get; set; } = new();
        public HashSet<string> AssignedMachines { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<PartRecord> Parts { get; set; } = new();
        public List<MachiningRecord> History { get; set; } = new();
        public MachiningRecord? ActiveJob { get; set; }
        public string? SelectedMaMay { get; set; }
        public long? SelectedPartId { get; set; }
        public string? Message { get; set; }
        public bool IsError { get; set; }
        public string? Lot { get; set; }
    }

    public class TaiKhoanEditViewModel
    {
        public long Id { get; set; }
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Password { get; set; }
        [Required]
        public string Role { get; set; } = "User";
        public bool IsActive { get; set; } = true;
        public List<string> SelectedMachines { get; set; } = new();
        public List<MachineRecord> AllMachines { get; set; } = new();
        public bool IsNew { get; set; } = true;
    }

    public class KpiReportViewModel
    {
        public string Period { get; set; } = "day";
        public DateTime FromDate { get; set; } = DateTime.Today;
        public DateTime ToDate { get; set; } = DateTime.Today;
        public string MaMay { get; set; } = "Tất cả";
        public string MaSp { get; set; } = "Tất cả";
        public List<string> MachineOptions { get; set; } = new() { "Tất cả" };
        public List<string> ProductOptions { get; set; } = new() { "Tất cả" };
        public int TongSanLuong { get; set; }
        public int SoLanGiaCong { get; set; }
        public int SanPhamDat { get; set; }
        public int SanPhamLoi { get; set; }
        public int SoSuCo { get; set; }
        public TimeSpan ThoiGianHoatDong { get; set; }
        public double HieuSuatPercent { get; set; }
        public List<MachiningRecord> MachiningRows { get; set; } = new();
        public List<AlarmItem> Alarms { get; set; } = new();
    }

    public class AlarmFilterViewModel
    {
        public string? MaMay { get; set; }
        public string? LoaiCanhBao { get; set; }
        public string? TrangThai { get; set; }
        public List<AlarmItem> Items { get; set; } = new();
        public List<string> MachineOptions { get; set; } = new();
        public int ActiveCount { get; set; }
    }

    public class MaintenanceEditViewModel
    {
        public long Id { get; set; }
        [Required, StringLength(50)]
        public string ThietBi { get; set; } = string.Empty;
        [Required, StringLength(100)]
        public string Loai { get; set; } = string.Empty;
        [DataType(DataType.Date)]
        public DateTime NgayBt { get; set; } = DateTime.Today;
        [DataType(DataType.Date)]
        public DateTime LanTiepTheo { get; set; } = DateTime.Today.AddMonths(1);
        [Required, StringLength(100)]
        public string NguoiThucHien { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "ĐÃ LÊN LỊCH";
        [StringLength(500)]
        public string? GhiChu { get; set; }
        public List<MachineRecord> Machines { get; set; } = new();
    }

    public class PinSettingsViewModel
    {
        [StringLength(6)]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "PIN mới phải gồm đúng 6 chữ số.")]
        public string AdminPin { get; set; } = string.Empty;

        [StringLength(6)]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "PIN mới phải gồm đúng 6 chữ số.")]
        public string UserPin { get; set; } = string.Empty;

        public bool ShowAdminNewPin { get; set; }
        public bool ShowUserNewPin { get; set; }
    }
}
