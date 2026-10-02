using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class DashboardService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly BangTaiModel plcState;
        private readonly BangTaiLine2Model plcState2;

        public DashboardService(IDbContextFactory<ProductionDbContext> contextFactory, BangTaiModel plcState, BangTaiLine2Model plcState2)
        {
            this.contextFactory = contextFactory;
            this.plcState = plcState;
            this.plcState2 = plcState2;
        }

        public DashboardMonitoringModel GetDashboardData(DateTime? from = null, DateTime? to = null)
        {
            using var db = contextFactory.CreateDbContext();

            from ??= DateTime.Today;
            to ??= DateTime.Now;

            var model = new DashboardMonitoringModel();
            model.Plc = GetPlcStatus();

            // Get Production Summary
            var productionToday = db.ProductionRecords
                .AsNoTracking()
                .Where(p => p.NgayGio >= from && p.NgayGio <= to)
                .ToList();

            model.SanXuat.TongSanLuong = productionToday.Count;
            model.SanXuat.SanLuongOK = productionToday.Count(p => p.KetQua == "OK");
            model.SanXuat.SanLuongNG = productionToday.Count(p => p.KetQua == "NG");
            model.SanXuat.ThoiGianChay = plcState.RunTime;
            model.SanXuat.ThoiGianDung = plcState.StopTime;

            // Get current production lot
            var currentLot = db.ProductionLots
                .AsNoTracking()
                .OrderByDescending(l => l.NgayBatDau)
                .FirstOrDefault(l => l.TrangThai == "RUNNING" || l.TrangThai == "WAITING");

            if (currentLot != null)
            {
                model.SanXuat.SanPhamHienTai = currentLot.MaSp;
                model.SanXuat.LoHienTai = currentLot.Lot;
            }

            // Get Device Status
            var machines = db.Machines.AsNoTracking().Where(item => item.IsActive).ToList();
            var devices = db.Devices
                .AsNoTracking()
                .Where(device => device.Ma == "BT01" || device.Ma == "BT02")
                .ToList();

            foreach (var device in devices)
            {
                model.ThietBi.Add(new DashboardDeviceStatus
                {
                    Ma = device.Ma,
                    Ten = device.Ten,
                    TrangThai = GetDeviceStatus(device, model.Plc),
                    Loai = device.Loai,
                    TocDo = GetLiveState(device.Ma)?.TocDo ?? 0,
                    UrlChiTiet = $"/Home/Device/{Uri.EscapeDataString(device.Ma)}"
                });
            }

            foreach (var machine in machines.Where(item => item.MaMay != "BT01" && item.MaMay != "BT02"))
            {
                model.ThietBi.Add(new DashboardDeviceStatus
                {
                    Ma = machine.MaMay,
                    Ten = machine.Ten,
                    TrangThai = machine.TrangThai,
                    Loai = "MACHINE",
                    TocDo = 0
                });
            }

            model.TongSoMay = machines.Count;
            model.SoMayRunning = machines.Count(d => d.TrangThai == "RUNNING");
            model.SoMayPaused = machines.Count(d => d.TrangThai == "PAUSED");
            model.SoMayStopped = machines.Count(d => d.TrangThai == "STOPPED");
            model.SoMayAlarm = machines.Count(d => d.TrangThai == "ALARM");
            model.SoThietBiHoatDong = model.SoMayRunning;
            model.TyLeHoatDong = model.TongSoMay == 0 ? 0 : Math.Round(model.SoMayRunning * 100d / model.TongSoMay, 1);

            // Get Sensor Status
            var sensors = db.Sensors
                .AsNoTracking()
                .ToList();

            foreach (var sensor in sensors)
            {
                // Get latest sensor reading
                var latestReading = db.SensorData
                    .AsNoTracking()
                    .Where(s => s.MaSensor == sensor.Ma)
                    .OrderByDescending(s => s.NgayGio)
                    .FirstOrDefault();

                model.CamBien.Add(new DashboardSensorStatus
                {
                    Ma = sensor.Ma,
                    Ten = sensor.Ten,
                    Loai = sensor.Loai,
                    GiaTri = latestReading?.GiaTri.ToString("F2") ?? "N/A",
                    IsActive = sensor.TrangThai == "ACTIVE",
                    DonVi = sensor.DonVi ?? ""
                });
            }

            model.SoCamBienHoatDong = model.CamBien.Count(c => c.IsActive);

            // Get Active Alarms
            var activeAlarms = db.AlarmRecords
                .AsNoTracking()
                .Where(a => a.TrangThai == "CHƯA XỬ LÝ")
                .OrderByDescending(a => a.NgayGio)
                .Take(10)
                .ToList();

            foreach (var alarm in activeAlarms)
            {
                model.BaoDong.Add(new AlarmItem
                {
                    MucDo = alarm.MucDo,
                    ThietBi = alarm.ThietBi,
                    NoiDung = alarm.NoiDung,
                    NgayGio = alarm.NgayGio,
                    DurationSeconds = alarm.DurationSeconds,
                    TrangThai = alarm.TrangThai,
                    ThoiGian = alarm.NgayGio.ToString("HH:mm:ss")
                });
            }

            model.SoBaoDongChuaXuLy = model.BaoDong.Count(a => a.TrangThai == "CHƯA XỬ LÝ");
            model.SanXuat.SoLanCanhBao = db.AlarmRecords.Count(a => a.NgayGio >= from && a.NgayGio <= to);

            // Get Maintenance Items
            var maintenanceItems = db.MaintenanceRecords
                .AsNoTracking()
                .ToList();

            foreach (var maint in maintenanceItems)
            {
                model.BaoTri.Add(new MaintenanceItem
                {
                    ThietBi = maint.ThietBi,
                    Loai = maint.Loai,
                    NgayBt = maint.NgayBt.ToString("dd/MM/yyyy"),
                    LanTiepTheo = maint.LanTiepTheo.ToString("dd/MM/yyyy"),
                    TrangThai = maint.TrangThai,
                    NguoiThucHien = maint.NguoiThucHien,
                    GhiChu = maint.GhiChu,
                    QuaHan = maint.LanTiepTheo.Date < DateTime.Today,
                    SapDenHan = maint.LanTiepTheo.Date >= DateTime.Today && maint.LanTiepTheo.Date <= DateTime.Today.AddDays(7)
                });
            }

            // Determine system status
            if (!model.Plc.IsConnected || model.Plc.IsStale)
            {
                model.TrangThaiHeThong = "OFFLINE";
            }
            else if (model.SoBaoDongChuaXuLy > 0)
            {
                model.TrangThaiHeThong = "WARNING";
            }
            else if (model.SanXuat.SanLuongNG > 0)
            {
                model.TrangThaiHeThong = "NORMAL";
            }
            else
            {
                model.TrangThaiHeThong = "NORMAL";
            }

            model.ThoiGianCapNhat = DateTime.Now;

            return model;
        }

        public DashboardPlcStatus GetPlcStatus()
        {
            var secondsSinceLastUpdate = plcState.PlcLastCommunication.HasValue
                ? Math.Max(0, (long)(DateTime.Now - plcState.PlcLastCommunication.Value).TotalSeconds)
                : (long?)null;

            return new DashboardPlcStatus
            {
                IsConnected = plcState.PlcOnline,
                LastCommunication = plcState.PlcLastCommunication,
                SecondsSinceLastUpdate = secondsSinceLastUpdate,
                IsStale = secondsSinceLastUpdate >= 10
            };
        }

        public DeviceDetailViewModel GetDeviceDetail(string deviceCode)
        {
            using var db = contextFactory.CreateDbContext();
            var plc = GetPlcStatus();
            var device = db.Devices.AsNoTracking()
                .FirstOrDefault(item => (item.Ma == "BT01" || item.Ma == "BT02") && item.Ma == deviceCode);
            if (device == null)
            {
                return new DeviceDetailViewModel { Plc = plc };
            }

            var production = db.ProductionRecords.AsNoTracking().Where(item => item.ThietBi == deviceCode && item.NgayGio >= DateTime.Today).ToList();
            var sensors = db.Sensors.AsNoTracking().OrderBy(item => item.Ma).ToList()
                .Select(sensor => new DashboardSensorStatus
                {
                    Ma = sensor.Ma,
                    Ten = sensor.Ten,
                    Loai = sensor.Loai,
                    GiaTri = db.SensorData.AsNoTracking().Where(item => item.MaSensor == sensor.Ma).OrderByDescending(item => item.NgayGio).Select(item => item.GiaTri.ToString("F2")).FirstOrDefault() ?? "N/A",
                    IsActive = sensor.TrangThai == "ACTIVE",
                    DonVi = sensor.DonVi ?? string.Empty
                }).ToList();
            var events = db.EventRecords.AsNoTracking().Where(item => item.ThietBi == deviceCode).OrderByDescending(item => item.NgayGio).Take(20)
                .Select(item => new EventLogItem { Timestamp = item.NgayGio, Message = item.NoiDung }).ToList();

            return new DeviceDetailViewModel
            {
                Plc = plc,
                ThietBi = new DashboardDeviceStatus
                {
                    Ma = device.Ma,
                    Ten = device.Ten,
                    Loai = device.Loai,
                    TrangThai = GetDeviceStatus(device, plc),
                    TocDo = GetLiveState(device.Ma)?.TocDo ?? 0,
                    UrlChiTiet = $"/Home/Device/{Uri.EscapeDataString(device.Ma)}"
                },
                CamBien = sensors,
                SanLuong = production.Count,
                LichSu = events
            };
        }

        private string GetDeviceStatus(DeviceRecord device, DashboardPlcStatus plc)
        {
            if (!plc.IsConnected || plc.IsStale)
            {
                return "OFFLINE";
            }

            var liveState = GetLiveState(device.Ma);
            if (liveState != null)
            {
                if (liveState.DungKhanCap || liveState.LoiMay || liveState.BaoLoi)
                {
                    return "ALARM";
                }

                return liveState.MotorRunning ? "RUNNING" : "STOPPED";
            }

            return device.TrangThai.ToUpperInvariant() switch
            {
                "RUN" => "RUNNING",
                "IDLE" => "STOPPED",
                "ERROR" => "ALARM",
                "MAINTENANCE" => "MAINTENANCE",
                "ALARM" => "ALARM",
                "RUNNING" => "RUNNING",
                _ => "STOPPED"
            };
        }

        private BangTaiModel? GetLiveState(string deviceCode)
        {
            return deviceCode switch
            {
                "BT01" => plcState,
                "BT02" => plcState2,
                _ => null
            };
        }

        public DashboardProductionSummary GetProductionSummary(DateTime? from = null, DateTime? to = null)
        {
            using var db = contextFactory.CreateDbContext();

            from ??= DateTime.Today;
            to ??= DateTime.Now;

            var records = db.ProductionRecords
                .AsNoTracking()
                .Where(p => p.NgayGio >= from && p.NgayGio <= to)
                .ToList();

            var summary = new DashboardProductionSummary
            {
                TongSanLuong = records.Count,
                SanLuongOK = records.Count(p => p.KetQua == "OK"),
                SanLuongNG = records.Count(p => p.KetQua == "NG")
            };

            var currentLot = db.ProductionLots
                .AsNoTracking()
                .OrderByDescending(l => l.NgayBatDau)
                .FirstOrDefault();

            if (currentLot != null)
            {
                summary.SanPhamHienTai = currentLot.MaSp;
                summary.LoHienTai = currentLot.Lot;
            }

            return summary;
        }

        public List<DashboardDeviceStatus> GetDeviceStatus()
        {
            using var db = contextFactory.CreateDbContext();
            var plc = GetPlcStatus();

            var devices = db.Devices
                .AsNoTracking()
                .Where(device => device.Ma == "BT01" || device.Ma == "BT02")
                .Select(d => new DashboardDeviceStatus
                {
                    Ma = d.Ma,
                    Ten = d.Ten,
                    TrangThai = GetDeviceStatus(d, plc),
                    Loai = d.Loai,
                    TocDo = d.Ma == "BT01" ? plcState.TocDo : d.Ma == "BT02" ? plcState2.TocDo : 0,
                    UrlChiTiet = $"/Home/Device/{Uri.EscapeDataString(d.Ma)}"
                })
                .ToList();

            devices.AddRange(db.Machines.AsNoTracking()
                .Where(item => item.IsActive && item.MaMay != "BT01" && item.MaMay != "BT02")
                .Select(item => new DashboardDeviceStatus
                {
                    Ma = item.MaMay,
                    Ten = item.Ten,
                    TrangThai = item.TrangThai,
                    Loai = "MACHINE",
                    TocDo = 0
                })
                .ToList());

            return devices;
        }

        public List<DashboardSensorStatus> GetSensorStatus()
        {
            using var db = contextFactory.CreateDbContext();

            var sensors = db.Sensors.AsNoTracking().ToList();
            var result = new List<DashboardSensorStatus>();

            foreach (var sensor in sensors)
            {
                var latestReading = db.SensorData
                    .AsNoTracking()
                    .Where(s => s.MaSensor == sensor.Ma)
                    .OrderByDescending(s => s.NgayGio)
                    .FirstOrDefault();

                result.Add(new DashboardSensorStatus
                {
                    Ma = sensor.Ma,
                    Ten = sensor.Ten,
                    Loai = sensor.Loai,
                    GiaTri = latestReading != null ? latestReading.GiaTri.ToString("F2") : "N/A",
                    IsActive = sensor.TrangThai == "ACTIVE",
                    DonVi = sensor.DonVi ?? ""
                });
            }

            return result;
        }

        public List<AlarmItem> GetRecentAlarms(int limit = 10)
        {
            using var db = contextFactory.CreateDbContext();

            return db.AlarmRecords
                .AsNoTracking()
                .OrderByDescending(a => a.NgayGio)
                .Take(limit)
                .Select(a => new AlarmItem
                {
                    MucDo = a.MucDo,
                    ThietBi = a.ThietBi,
                    NoiDung = a.NoiDung,
                    NgayGio = a.NgayGio,
                    DurationSeconds = a.DurationSeconds,
                    TrangThai = a.TrangThai,
                    ThoiGian = a.NgayGio.ToString("HH:mm:ss")
                })
                .ToList();
        }
    }
}
