using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class NhaMayDataService
    {
        private readonly BangTaiModel heThong;
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly SystemConfig cauHinh = new();
        private readonly SystemConfig cauHinh2 = new();

        public NhaMayDataService(BangTaiModel heThong, IDbContextFactory<ProductionDbContext> contextFactory)
        {
            this.heThong = heThong;
            this.contextFactory = contextFactory;
            EnsureReferenceData();
            LoadConfig();
        }

        public SystemConfig GetConfig() => cauHinh;
        public SystemConfig GetConfig(string deviceCode) => deviceCode == "BT02" ? cauHinh2 : cauHinh;

        private void EnsureReferenceData()
        {
            using var db = contextFactory.CreateDbContext();
            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS DeviceConfigs (ThietBi VARCHAR(50) NOT NULL, TocDoMacDinh INT NOT NULL DEFAULT 50, TocDoToiDa INT NOT NULL DEFAULT 100, DelayCamBien INT NOT NULL DEFAULT 2, TuDongKhoiDong TINYINT(1) NOT NULL DEFAULT 1, ChoPhepChay TINYINT(1) NOT NULL DEFAULT 1, ChuKyLuuSensor INT NOT NULL DEFAULT 10, ThoiGianCanhBao INT NOT NULL DEFAULT 0, PRIMARY KEY (ThietBi))");
            var productionDeviceColumnExists = db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS `Value` FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ProductionRecords' AND COLUMN_NAME = 'ThietBi'")
                .Single() > 0;
            if (!productionDeviceColumnExists)
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE ProductionRecords ADD COLUMN ThietBi VARCHAR(50) NOT NULL DEFAULT 'BT01'");
            }
            if (!db.SystemConfigs.Any())
            {
                db.SystemConfigs.Add(new SystemConfigRecord { Id = 1, TocDoMacDinh = cauHinh.TocDoMacDinh, TocDoToiDa = cauHinh.TocDoToiDa, DelayCamBien = cauHinh.DelayCamBien, ChuKyLuuSensor = cauHinh.ChuKyLuuSensor, ThoiGianCanhBao = cauHinh.ThoiGianCanhBao, TuDongKhoiDong = cauHinh.TuDongKhoiDong, ChoPhepChay = cauHinh.ChoPhepChay });
            }
            if (!db.Devices.Any(item => item.Ma == "BT02"))
            {
                db.Devices.Add(new DeviceRecord { Ma = "BT02", Ten = "Băng tải 02", Loai = "CONVEYOR", ViTri = "Line 2", MoTa = "Băng tải phụ" });
            }
            foreach (var deviceCode in new[] { "BT01", "BT02" })
            {
                if (!db.DeviceConfigs.Any(item => item.ThietBi == deviceCode))
                {
                    db.DeviceConfigs.Add(new DeviceConfigRecord { ThietBi = deviceCode, TocDoMacDinh = cauHinh.TocDoMacDinh, TocDoToiDa = cauHinh.TocDoToiDa, DelayCamBien = cauHinh.DelayCamBien, ChuKyLuuSensor = cauHinh.ChuKyLuuSensor, ThoiGianCanhBao = cauHinh.ThoiGianCanhBao, TuDongKhoiDong = cauHinh.TuDongKhoiDong, ChoPhepChay = cauHinh.ChoPhepChay });
                }
            }
            var sensorDefaults = new[]
            {
                new SensorRecord { Ma = "CB01", Ten = "Phát hiện sản phẩm", Loai = "Quang" },
                new SensorRecord { Ma = "CB02", Ten = "Vị trí", Loai = "Quang" },
                new SensorRecord { Ma = "CB03", Ten = "Nhận diện loại sản phẩm", Loai = "Phân loại" },
                new SensorRecord { Ma = "CB04", Ten = "Cuối băng tải", Loai = "Quang" },
                new SensorRecord { Ma = "CB05", Ten = "Nhiệt độ", Loai = "Nhiệt" },
                new SensorRecord { Ma = "CB06", Ten = "Trọng lượng", Loai = "Cân" }
            };
            var existingSensors = db.Sensors.ToDictionary(item => item.Ma);
            foreach (var sensor in sensorDefaults)
            {
                if (existingSensors.TryGetValue(sensor.Ma, out var existing))
                {
                    existing.Ten = sensor.Ten;
                    existing.Loai = sensor.Loai;
                    existing.TrangThai = sensor.TrangThai;
                }
                else
                {
                    db.Sensors.Add(sensor);
                }
            }
            if (!db.MaintenanceRecords.Any())
            {
                db.MaintenanceRecords.AddRange(
                    new MaintenanceRecord { ThietBi = "BT01", Loai = "Định kỳ", NgayBt = new DateTime(2026, 8, 15), LanTiepTheo = new DateTime(2026, 9, 15), TrangThai = "OK" },
                    new MaintenanceRecord { ThietBi = "CB02", Loai = "Định kỳ", NgayBt = new DateTime(2026, 8, 10), LanTiepTheo = new DateTime(2026, 9, 10), TrangThai = "OK" },
                    new MaintenanceRecord { ThietBi = "M01", Loai = "Sửa chữa", NgayBt = new DateTime(2026, 8, 1), LanTiepTheo = new DateTime(2026, 11, 1), TrangThai = "SẮP ĐẾN" });
            }
            if (!db.AlarmRecords.Any())
            {
                db.AlarmRecords.AddRange(
                    new AlarmRecord { MucDo = "ERROR", ThietBi = "BT01", NoiDung = "Quá tải", NgayGio = DateTime.Today.AddHours(8.5), DurationSeconds = 300, TrangThai = "CHƯA XỬ LÝ" },
                    new AlarmRecord { MucDo = "WARN", ThietBi = "CB02", NoiDung = "Mất tín hiệu", NgayGio = DateTime.Today.AddHours(8).AddMinutes(25), DurationSeconds = 60, TrangThai = "ĐÃ XÁC NHẬN" },
                    new AlarmRecord { MucDo = "INFO", ThietBi = "M01", NoiDung = "Hoàn tất phân loại", NgayGio = DateTime.Today.AddHours(8).AddMinutes(20), DurationSeconds = 0, TrangThai = "ĐÃ XỬ LÝ" });
            }
            db.SaveChanges();
        }

        private void LoadConfig()
        {
            using var db = contextFactory.CreateDbContext();
            var saved = db.SystemConfigs.Single(item => item.Id == 1);
            cauHinh.TocDoMacDinh = saved.TocDoMacDinh;
            cauHinh.TocDoToiDa = saved.TocDoToiDa;
            cauHinh.DelayCamBien = saved.DelayCamBien;
            cauHinh.ChuKyLuuSensor = saved.ChuKyLuuSensor;
            cauHinh.ThoiGianCanhBao = saved.ThoiGianCanhBao;
            cauHinh.TuDongKhoiDong = saved.TuDongKhoiDong;
            cauHinh.ChoPhepChay = saved.ChoPhepChay;
            heThong.TocDoToiDa = cauHinh.TocDoToiDa;
            heThong.TocDo = Math.Min(cauHinh.TocDoMacDinh, heThong.TocDoToiDa);
            var saved2 = db.DeviceConfigs.Single(item => item.ThietBi == "BT02");
            cauHinh2.TocDoMacDinh = saved2.TocDoMacDinh;
            cauHinh2.TocDoToiDa = saved2.TocDoToiDa;
            cauHinh2.DelayCamBien = saved2.DelayCamBien;
            cauHinh2.ChuKyLuuSensor = saved2.ChuKyLuuSensor;
            cauHinh2.ThoiGianCanhBao = saved2.ThoiGianCanhBao;
            cauHinh2.TuDongKhoiDong = saved2.TuDongKhoiDong;
            cauHinh2.ChoPhepChay = saved2.ChoPhepChay;
        }

        public void SaveConfig(SystemConfig config)
        {
            cauHinh.TocDoMacDinh = config.TocDoMacDinh;
            cauHinh.TocDoToiDa = config.TocDoToiDa;
            cauHinh.DelayCamBien = config.DelayCamBien;
            cauHinh.ChuKyLuuSensor = config.ChuKyLuuSensor;
            cauHinh.ThoiGianCanhBao = config.ThoiGianCanhBao;
            cauHinh.TuDongKhoiDong = config.TuDongKhoiDong;
            cauHinh.ChoPhepChay = config.ChoPhepChay;
            heThong.TocDoToiDa = config.TocDoToiDa;
            heThong.TocDo = Math.Min(config.TocDoMacDinh, heThong.TocDoToiDa);
            using var db = contextFactory.CreateDbContext();
            var saved = db.SystemConfigs.Single(item => item.Id == 1);
            saved.TocDoMacDinh = config.TocDoMacDinh;
            saved.TocDoToiDa = config.TocDoToiDa;
            saved.DelayCamBien = config.DelayCamBien;
            saved.ChuKyLuuSensor = config.ChuKyLuuSensor;
            saved.ThoiGianCanhBao = config.ThoiGianCanhBao;
            saved.TuDongKhoiDong = config.TuDongKhoiDong;
            saved.ChoPhepChay = config.ChoPhepChay;
            db.SaveChanges();
            heThong.AddEvent("Lưu cấu hình hệ thống");
        }

        public void SaveConfig(SystemConfig config, string deviceCode)
        {
            if (deviceCode != "BT02")
            {
                SaveConfig(config);
                return;
            }

            cauHinh2.TocDoMacDinh = config.TocDoMacDinh;
            cauHinh2.TocDoToiDa = config.TocDoToiDa;
            cauHinh2.DelayCamBien = config.DelayCamBien;
            cauHinh2.ChuKyLuuSensor = config.ChuKyLuuSensor;
            cauHinh2.ThoiGianCanhBao = config.ThoiGianCanhBao;
            cauHinh2.TuDongKhoiDong = config.TuDongKhoiDong;
            cauHinh2.ChoPhepChay = config.ChoPhepChay;
            using var db = contextFactory.CreateDbContext();
            var saved = db.DeviceConfigs.Single(item => item.ThietBi == "BT02");
            saved.TocDoMacDinh = config.TocDoMacDinh;
            saved.TocDoToiDa = config.TocDoToiDa;
            saved.DelayCamBien = config.DelayCamBien;
            saved.ChuKyLuuSensor = config.ChuKyLuuSensor;
            saved.ThoiGianCanhBao = config.ThoiGianCanhBao;
            saved.TuDongKhoiDong = config.TuDongKhoiDong;
            saved.ChoPhepChay = config.ChoPhepChay;
            db.SaveChanges();
        }

        public HmiDashboardViewModel GetDashboard()
        {
            var trangThai = heThong.DungKhanCap || heThong.LoiMay ? "HỆ THỐNG ĐANG BÁO ĐỘNG" : heThong.MotorRunning ? "HỆ THỐNG ĐANG HOẠT ĐỘNG" : "HỆ THỐNG ĐANG DỪNG";
            return new HmiDashboardViewModel { HeThong = heThong, CamBiens = GetSensors(), BaoDongs = GetAlarms(), TrangThaiNhaMay = trangThai };
        }

        public List<SensorItem> GetSensors()
        {
            using var db = contextFactory.CreateDbContext();
            return db.Sensors.AsNoTracking().OrderBy(item => item.Ma).ToList().Select(sensor => new SensorItem
            {
                Ma = sensor.Ma, Ten = sensor.Ten, Loai = sensor.Loai, TrangThai = sensor.TrangThai, DonVi = sensor.DonVi, MoTa = sensor.MoTa,
                GiaTri = sensor.Ma switch
                {
                    "CB01" => heThong.CamBien ? "ON" : "OFF",
                    "CB02" => heThong.MotorRunning ? "ON" : "OFF",
                    "CB03" => heThong.XyLanhA ? "A" : heThong.XyLanhB ? "B" : "-",
                    "CB04" => heThong.BaoDay ? "ON" : "OFF",
                    "CB05" => $"{heThong.NhietDo:0}°C",
                    "CB06" => "1.2kg",
                    _ => "-"
                },
                IsOn = sensor.Ma switch
                {
                    "CB01" => heThong.CamBien,
                    "CB02" => heThong.MotorRunning,
                    "CB03" => heThong.XyLanhA || heThong.XyLanhB,
                    "CB04" => heThong.BaoDay,
                    _ => true
                }
            }).ToList();
        }

        public List<AlarmItem> GetAlarms()
        {
            using var db = contextFactory.CreateDbContext();
            var result = db.AlarmRecords.AsNoTracking().OrderByDescending(item => item.NgayGio).ToList().Select(item => new AlarmItem
            {
                Id = item.Id,
                MucDo = item.MucDo,
                ThietBi = item.ThietBi,
                NoiDung = item.NoiDung,
                ThoiGian = item.NgayGio.ToString("HH:mm"),
                NgayGio = item.NgayGio,
                NgayGioKetThuc = item.NgayGioKetThuc,
                DurationSeconds = item.DurationSeconds,
                TrangThai = item.TrangThai,
                LoaiCanhBao = item.LoaiCanhBao
            }).ToList();
            // E-STOP và lỗi máy đã được BangTaiServices lưu vào AlarmRecords.
            // Chỉ thêm cảnh báo Buffer đầy vì đây là trạng thái tạm thời chưa có bản ghi DB.
            if (heThong.BaoDay) result.Insert(0, new AlarmItem { MucDo = "WARN", ThietBi = "BT01", NoiDung = "Buffer đầy", ThoiGian = DateTime.Now.ToString("HH:mm"), NgayGio = DateTime.Now, TrangThai = "CHƯA XỬ LÝ", LoaiCanhBao = "BUFFER" });
            return result;
        }

        public List<MaintenanceItem> GetMaintenance()
        {
            using var db = contextFactory.CreateDbContext();
            var today = DateTime.Today;
            return db.MaintenanceRecords.AsNoTracking().OrderBy(item => item.LanTiepTheo).ToList().Select(item => new MaintenanceItem
            {
                Id = item.Id, ThietBi = item.ThietBi, Loai = item.Loai, NgayBt = item.NgayBt.ToString("dd/MM/yyyy"), LanTiepTheo = item.LanTiepTheo.ToString("dd/MM/yyyy"), TrangThai = item.TrangThai, NguoiThucHien = item.NguoiThucHien, GhiChu = item.GhiChu, QuaHan = item.LanTiepTheo.Date < today, SapDenHan = item.LanTiepTheo.Date >= today && item.LanTiepTheo.Date <= today.AddDays(7)
            }).ToList();
        }

        public void ConfirmAllAlarms()
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
        }
    }
}
