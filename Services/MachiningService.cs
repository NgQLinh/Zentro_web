using System.Data;
using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class MachiningService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly MachineService machineService;
        private readonly ProductionDatabaseService database;

        public MachiningService(
            IDbContextFactory<ProductionDbContext> contextFactory,
            MachineService machineService,
            ProductionDatabaseService database)
        {
            this.contextFactory = contextFactory;
            this.machineService = machineService;
            this.database = database;
        }

        public MachiningRecord? GetActiveJob(string maMay)
        {
            using var db = contextFactory.CreateDbContext();
            return db.MachiningRecords.AsNoTracking()
                .FirstOrDefault(item => item.MaMay == maMay && item.TrangThai == "RUNNING");
        }

        public List<MachiningRecord> GetHistory(string? maMay = null, long? userId = null, int limit = 100)
        {
            using var db = contextFactory.CreateDbContext();
            var query = db.MachiningRecords.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(maMay))
            {
                query = query.Where(item => item.MaMay == maMay);
            }

            if (userId.HasValue)
            {
                query = query.Where(item => item.UserId == userId.Value);
            }

            return query.OrderByDescending(item => item.ThoiGianBatDau).Take(limit).ToList();
        }

        public (bool Ok, string Message, MachiningRecord? Record) Start(string maMay, long partId, long userId, string username, string? lot = null)
        {
            using var db = contextFactory.CreateDbContext();
            using var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable);
            var machine = db.Machines.FirstOrDefault(item => item.MaMay == maMay);
            if (machine == null || !machine.IsActive)
            {
                return (false, "Máy không tồn tại hoặc đã ngưng hoạt động.", null);
            }

            if (machine.TrangThai is "ALARM" or "MAINTENANCE")
            {
                return (false, $"Không thể bắt đầu: máy đang ở trạng thái {machine.TrangThai}.", null);
            }

            if (db.MachiningRecords.Any(item => item.MaMay == maMay && item.TrangThai == "RUNNING"))
            {
                return (false, "Máy đang gia công", null);
            }

            var part = db.Parts.FirstOrDefault(item => item.Id == partId);
            if (part == null)
            {
                return (false, "Không tìm thấy Part.", null);
            }

            if (!string.IsNullOrWhiteSpace(part.MaMay) && !string.Equals(part.MaMay, maMay, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Part này không được phân công cho máy đã chọn.", null);
            }

            var now = DateTime.Now;
            var record = new MachiningRecord
            {
                MaMay = maMay,
                PartId = part.Id,
                MaSp = part.MaSp,
                MaIndex = part.MaIndex,
                TenChiTiet = part.TenChiTiet,
                UserId = userId,
                Username = username,
                ThoiGianBatDau = now,
                TrangThai = "RUNNING",
                Lot = string.IsNullOrWhiteSpace(lot) ? null : lot
            };

            if (!string.IsNullOrWhiteSpace(record.Lot) && db.ProductionLots.Any(item => item.Lot == record.Lot))
            {
                var production = new ProductionRecord
                {
                    ThietBi = maMay,
                    MaSp = part.MaSp,
                    Loai = part.TenChiTiet,
                    KetQua = "PROCESS",
                    NgayGio = now,
                    Ca = GetShift(now),
                    Lot = record.Lot!,
                    CycleTimeSeconds = 0
                };
                db.ProductionRecords.Add(production);
                db.SaveChanges();
                record.ProductionRecordId = production.Id;
            }

            db.MachiningRecords.Add(record);
            db.SaveChanges();
            transaction.Commit();

            machineService.SetStatus(maMay, "RUNNING", "UI", $"Bắt đầu gia công {part.MaIndex}");
            database.SaveEvent(new EventRecord
            {
                NgayGio = now,
                NoiDung = $"Bắt đầu gia công {part.MaIndex} / {part.TenChiTiet} trên {maMay}",
                LoaiSuKien = "MACHINING_START",
                ThietBi = maMay
            });

            return (true, "Đã bắt đầu gia công.", record);
        }

        public (bool Ok, string Message) End(long machiningId, string ketQua, long userId)
        {
            using var db = contextFactory.CreateDbContext();
            var record = db.MachiningRecords.FirstOrDefault(item => item.Id == machiningId);
            if (record == null)
            {
                return (false, "Không tìm thấy bản ghi gia công.");
            }

            if (record.TrangThai != "RUNNING")
            {
                return (false, "Công việc đã kết thúc.");
            }

            if (record.UserId != userId)
            {
                return (false, "Bạn không được kết thúc công việc của nhân viên khác.");
            }

            var now = DateTime.Now;
            record.ThoiGianKetThuc = now;
            record.TrangThai = "COMPLETED";
            record.KetQua = ketQua;
            record.CycleTimeSeconds = Math.Max(0, (now - record.ThoiGianBatDau).TotalSeconds);

            if (record.ProductionRecordId.HasValue)
            {
                var production = db.ProductionRecords.FirstOrDefault(item => item.Id == record.ProductionRecordId.Value);
                if (production != null)
                {
                    production.KetQua = ketQua == "NG" || ketQua == "Lỗi" ? "Lỗi" : "OK";
                    production.NgayGioKetThuc = now;
                    production.CycleTimeSeconds = record.CycleTimeSeconds;
                }

                if (!string.IsNullOrWhiteSpace(record.Lot))
                {
                    var lot = db.ProductionLots.FirstOrDefault(item => item.Lot == record.Lot);
                    if (lot != null)
                    {
                        lot.SoLuongThucTe += 1;
                        if (production?.KetQua == "Lỗi")
                        {
                            lot.SoLuongNG += 1;
                        }
                        else
                        {
                            lot.SoLuongOK += 1;
                        }
                    }
                }
            }

            db.SaveChanges();
            machineService.SetStatus(record.MaMay, "STOPPED", "UI", $"Kết thúc gia công {record.MaIndex}");
            database.SaveEvent(new EventRecord
            {
                NgayGio = now,
                NoiDung = $"Kết thúc gia công {record.MaIndex} trên {record.MaMay}: {ketQua}",
                LoaiSuKien = "MACHINING_END",
                ThietBi = record.MaMay
            });

            return (true, "Đã kết thúc gia công.");
        }

        private static string GetShift(DateTime time)
        {
            var hour = time.Hour;
            if (hour >= 6 && hour < 14) return "CA 1";
            if (hour >= 14 && hour < 22) return "CA 2";
            return "CA 3";
        }
    }
}
