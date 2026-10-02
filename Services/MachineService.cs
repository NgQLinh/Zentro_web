using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class MachineService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly ProductionDatabaseService database;

        public MachineService(IDbContextFactory<ProductionDbContext> contextFactory, ProductionDatabaseService database)
        {
            this.contextFactory = contextFactory;
            this.database = database;
        }

        public List<MachineRecord> GetAll(bool activeOnly = false)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.Machines.AsNoTracking().AsQueryable();
            if (activeOnly)
            {
                query = query.Where(item => item.IsActive);
            }

            return query.OrderBy(item => item.MaMay).ToList();
        }

        public MachineRecord? Get(string maMay)
        {
            using var db = contextFactory.CreateDbContext();
            return db.Machines.AsNoTracking().FirstOrDefault(item => item.MaMay == maMay);
        }

        public List<MachineStatusHistoryRecord> GetStatusHistory(string? maMay = null, DateTime? from = null, DateTime? to = null, int limit = 100)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.MachineStatusHistory.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(maMay))
            {
                query = query.Where(item => item.MaMay == maMay);
            }
            if (from.HasValue)
            {
                query = query.Where(item => item.ThoiDiem < (to ?? DateTime.Now) && (item.ThoiDiemKetThuc ?? DateTime.Now) >= from.Value.Date);
            }
            if (to.HasValue)
            {
                query = query.Where(item => item.ThoiDiem < to.Value.Date.AddDays(1));
            }

            return query.OrderByDescending(item => item.ThoiDiem).Take(limit).ToList();
        }

        public void Save(MachineRecord machine, bool isNew)
        {
            using var db = contextFactory.CreateDbContext();
            if (isNew)
            {
                if (db.Machines.Any(item => item.MaMay == machine.MaMay))
                {
                    throw new InvalidOperationException("Mã máy đã tồn tại.");
                }

                machine.TrangThaiCapNhatLuc = DateTime.Now;
                db.Machines.Add(machine);
                db.MachineStatusHistory.Add(new MachineStatusHistoryRecord
                {
                    MaMay = machine.MaMay,
                    TrangThaiMoi = machine.TrangThai,
                    ThoiDiem = DateTime.Now,
                    Nguon = "SYSTEM",
                    GhiChu = "Khởi tạo máy"
                });
                if (!db.Devices.Any(item => item.Ma == machine.MaMay))
                {
                    db.Devices.Add(new DeviceRecord
                    {
                        Ma = machine.MaMay,
                        Ten = machine.Ten,
                        Loai = "MACHINE",
                        ViTri = machine.BangTaiMap,
                        MoTa = $"Máy gia công {machine.MaMay}"
                    });
                }
            }
            else
            {
                var existing = db.Machines.First(item => item.MaMay == machine.MaMay);
                existing.Ten = machine.Ten;
                existing.IpPlc = machine.IpPlc;
                existing.PortPlc = machine.PortPlc;
                existing.IsActive = machine.IsActive;
                existing.BangTaiMap = machine.BangTaiMap;
                var device = db.Devices.FirstOrDefault(item => item.Ma == machine.MaMay);
                if (device != null)
                {
                    device.Ten = machine.Ten;
                }
            }

            db.SaveChanges();
        }

        public void Delete(string maMay)
        {
            using var db = contextFactory.CreateDbContext();
            if (db.MachiningRecords.Any(item => item.MaMay == maMay && item.TrangThai == "RUNNING"))
            {
                throw new InvalidOperationException("Không thể xóa máy đang gia công.");
            }

            var machine = db.Machines.FirstOrDefault(item => item.MaMay == maMay);
            if (machine == null)
            {
                return;
            }

            db.UserMachineAssignments.RemoveRange(db.UserMachineAssignments.Where(item => item.MaMay == maMay));
            db.Machines.Remove(machine);
            db.SaveChanges();
        }

        public void SetStatus(string maMay, string newStatus, string source, string? note = null, string? reason = null, string? performer = null)
        {
            using var db = contextFactory.CreateDbContext();
            var machine = db.Machines.FirstOrDefault(item => item.MaMay == maMay);
            if (machine == null)
            {
                return;
            }

            if (string.Equals(machine.TrangThai, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var now = DateTime.Now;
            var oldStatus = machine.TrangThai;
            var previous = db.MachineStatusHistory
                .Where(item => item.MaMay == maMay && item.ThoiDiemKetThuc == null)
                .OrderByDescending(item => item.ThoiDiem)
                .FirstOrDefault();
            if (previous != null)
            {
                previous.ThoiDiemKetThuc = now;
                previous.ThoiLuongGiay = Math.Max(0, (int)(now - previous.ThoiDiem).TotalSeconds);
            }
            machine.TrangThai = newStatus;
            machine.TrangThaiCapNhatLuc = now;
            db.MachineStatusHistory.Add(new MachineStatusHistoryRecord
            {
                MaMay = maMay,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = newStatus,
                ThoiDiem = now,
                Nguon = source,
                LyDoDung = newStatus is "STOPPED" or "PAUSED" ? reason : null,
                NguoiThucHien = performer,
                GhiChu = note
            });
            db.SaveChanges();

            if (newStatus == "ALARM")
            {
                database.SaveAlarm(new AlarmRecord
                {
                    MucDo = "ERROR",
                    ThietBi = maMay,
                    NoiDung = note ?? $"Máy {maMay} chuyển sang {newStatus}",
                    NgayGio = now,
                    TrangThai = "CHƯA XỬ LÝ",
                    LoaiCanhBao = "MAY_ALARM"
                });
            }

            database.SaveEvent(new EventRecord
            {
                NgayGio = now,
                NoiDung = $"Máy {maMay}: {oldStatus} → {newStatus}",
                LoaiSuKien = "MACHINE_STATUS",
                ThietBi = maMay
            });
        }

        public void SetPlcOnline(string maMay, bool online)
        {
            using var db = contextFactory.CreateDbContext();
            var machine = db.Machines.FirstOrDefault(item => item.MaMay == maMay);
            if (machine == null || machine.PlcOnline == online)
            {
                return;
            }

            machine.PlcOnline = online;
            db.SaveChanges();
        }

        public TimeSpan GetRunningTime(string? maMay, DateTime from, DateTime to)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.MachineStatusHistory.AsNoTracking()
                .Where(item => item.ThoiDiem >= from && item.ThoiDiem < to);
            if (!string.IsNullOrWhiteSpace(maMay) && maMay != "Tất cả")
            {
                query = query.Where(item => item.MaMay == maMay);
            }

            var events = query.OrderBy(item => item.ThoiDiem).ToList();
            if (events.Count == 0)
            {
                return TimeSpan.Zero;
            }

            double seconds = 0;
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].TrangThaiMoi != "RUNNING")
                {
                    continue;
                }

                var end = i + 1 < events.Count ? events[i + 1].ThoiDiem : to;
                if (end > to) end = to;
                if (end > events[i].ThoiDiem)
                {
                    seconds += (end - events[i].ThoiDiem).TotalSeconds;
                }
            }

            return TimeSpan.FromSeconds(seconds);
        }
    }
}
