using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Zentro.Data;
using Zentro.Models;

namespace Zentro.Services
{
    public class DatabaseBootstrapService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly IConfiguration configuration;
        private readonly PasswordHasher<UserRecord> passwordHasher = new();

        public DatabaseBootstrapService(IDbContextFactory<ProductionDbContext> contextFactory, IConfiguration configuration)
        {
            this.contextFactory = contextFactory;
            this.configuration = configuration;
        }

        public void EnsureSchemaAndSeed()
        {
            using var db = contextFactory.CreateDbContext();
            EnsureTables(db);
            EnsureMaintenanceHistoryTable(db);
            EnsureMachineHistoryColumns(db);
            EnsureAlarmColumns(db);
            EnsureMaintenanceColumns(db);
            EnsureActionLogTable(db);
            EnsureAuthenticationSettings(db);
            SeedDevices(db);
            SeedMachines(db);
            SeedUsers(db);
            SeedParts(db);
            db.SaveChanges();
        }

        private static void EnsureTables(ProductionDbContext db)
        {
            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS Machines (
                  MaMay VARCHAR(50) NOT NULL,
                  Ten VARCHAR(100) NOT NULL,
                  IpPlc VARCHAR(50) NULL,
                  PortPlc INT NOT NULL DEFAULT 502,
                  TrangThai VARCHAR(30) NOT NULL DEFAULT 'STOPPED',
                  IsActive TINYINT(1) NOT NULL DEFAULT 1,
                  BangTaiMap VARCHAR(50) NULL,
                  TrangThaiCapNhatLuc DATETIME(6) NULL,
                  PlcOnline TINYINT(1) NOT NULL DEFAULT 0,
                  PRIMARY KEY (MaMay),
                  INDEX IX_Machines_TrangThai (TrangThai)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS MachineStatusHistory (
                  Id BIGINT NOT NULL AUTO_INCREMENT,
                  MaMay VARCHAR(50) NOT NULL,
                  TrangThaiCu VARCHAR(30) NULL,
                  TrangThaiMoi VARCHAR(30) NOT NULL,
                  ThoiDiem DATETIME(6) NOT NULL,
                  ThoiDiemKetThuc DATETIME(6) NULL,
                  ThoiLuongGiay INT NULL,
                  Nguon VARCHAR(20) NOT NULL DEFAULT 'SYSTEM',
                  LyDoDung VARCHAR(100) NULL,
                  NguoiThucHien VARCHAR(100) NULL,
                  GhiChu VARCHAR(255) NULL,
                  PRIMARY KEY (Id),
                  INDEX IX_MachineStatusHistory_MaMay (MaMay),
                  INDEX IX_MachineStatusHistory_ThoiDiem (ThoiDiem),
                  CONSTRAINT FK_MachineStatusHistory_Machines FOREIGN KEY (MaMay) REFERENCES Machines(MaMay) ON UPDATE CASCADE ON DELETE CASCADE
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS Parts (
                  Id BIGINT NOT NULL AUTO_INCREMENT,
                  MaIndex VARCHAR(50) NOT NULL,
                  MaSp VARCHAR(50) NOT NULL,
                  TenChiTiet VARCHAR(150) NOT NULL,
                  MaMay VARCHAR(50) NULL,
                  CreatedAt DATETIME(6) NOT NULL,
                  PRIMARY KEY (Id),
                  INDEX IX_Parts_MaIndex (MaIndex),
                  INDEX IX_Parts_MaSp (MaSp),
                  INDEX IX_Parts_CreatedAt (CreatedAt)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS Users (
                  Id BIGINT NOT NULL AUTO_INCREMENT,
                  Username VARCHAR(50) NOT NULL,
                  PasswordHash VARCHAR(500) NOT NULL,
                  Role VARCHAR(30) NOT NULL DEFAULT 'User',
                  IsActive TINYINT(1) NOT NULL DEFAULT 1,
                  CreatedAt DATETIME(6) NOT NULL,
                  PRIMARY KEY (Id),
                  UNIQUE INDEX IX_Users_Username (Username)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS UserMachineAssignments (
                  Id BIGINT NOT NULL AUTO_INCREMENT,
                  UserId BIGINT NOT NULL,
                  MaMay VARCHAR(50) NOT NULL,
                  PRIMARY KEY (Id),
                  UNIQUE INDEX IX_UserMachineAssignments_User_Machine (UserId, MaMay),
                  CONSTRAINT FK_UserMachineAssignments_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON UPDATE CASCADE ON DELETE CASCADE,
                  CONSTRAINT FK_UserMachineAssignments_Machines FOREIGN KEY (MaMay) REFERENCES Machines(MaMay) ON UPDATE CASCADE ON DELETE CASCADE
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS MachiningRecords (
                  Id BIGINT NOT NULL AUTO_INCREMENT,
                  MaMay VARCHAR(50) NOT NULL,
                  PartId BIGINT NOT NULL,
                  MaSp VARCHAR(50) NOT NULL,
                  MaIndex VARCHAR(50) NOT NULL,
                  TenChiTiet VARCHAR(150) NOT NULL,
                  UserId BIGINT NOT NULL,
                  Username VARCHAR(50) NULL,
                  ThoiGianBatDau DATETIME(6) NOT NULL,
                  ThoiGianKetThuc DATETIME(6) NULL,
                  TrangThai VARCHAR(30) NOT NULL DEFAULT 'RUNNING',
                  KetQua VARCHAR(20) NULL,
                  Lot VARCHAR(50) NULL,
                  ProductionRecordId BIGINT NULL,
                  CycleTimeSeconds DOUBLE NOT NULL DEFAULT 0,
                  PRIMARY KEY (Id),
                  INDEX IX_MachiningRecords_MaMay (MaMay),
                  INDEX IX_MachiningRecords_Start (ThoiGianBatDau),
                  INDEX IX_MachiningRecords_MaSp (MaSp),
                  INDEX IX_MachiningRecords_TrangThai (TrangThai),
                  CONSTRAINT FK_MachiningRecords_Machines FOREIGN KEY (MaMay) REFERENCES Machines(MaMay) ON UPDATE CASCADE ON DELETE RESTRICT,
                  CONSTRAINT FK_MachiningRecords_Parts FOREIGN KEY (PartId) REFERENCES Parts(Id) ON UPDATE CASCADE ON DELETE RESTRICT,
                  CONSTRAINT FK_MachiningRecords_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON UPDATE CASCADE ON DELETE RESTRICT
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS PLCData (
                  Id BIGINT NOT NULL AUTO_INCREMENT,
                  MaMay VARCHAR(50) NOT NULL,
                  Tag VARCHAR(50) NOT NULL,
                  Address VARCHAR(50) NULL,
                  GiaTri DOUBLE NOT NULL,
                  NgayGio DATETIME(6) NOT NULL,
                  PRIMARY KEY (Id),
                  INDEX IX_PLCData_MaMay (MaMay),
                  INDEX IX_PLCData_NgayGio (NgayGio),
                  CONSTRAINT FK_PLCData_Machines FOREIGN KEY (MaMay) REFERENCES Machines(MaMay) ON UPDATE CASCADE ON DELETE CASCADE
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);
        }

        private static void EnsureAlarmColumns(ProductionDbContext db)
        {
            var endExists = db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS `Value` FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AlarmRecords' AND COLUMN_NAME = 'NgayGioKetThuc'")
                .Single() > 0;
            if (!endExists)
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE AlarmRecords ADD COLUMN NgayGioKetThuc DATETIME(6) NULL AFTER NgayGio");
            }

            var typeExists = db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS `Value` FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AlarmRecords' AND COLUMN_NAME = 'LoaiCanhBao'")
                .Single() > 0;
            if (!typeExists)
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE AlarmRecords ADD COLUMN LoaiCanhBao VARCHAR(50) NULL AFTER TrangThai");
            }
        }

                private static void EnsureMaintenanceHistoryTable(ProductionDbContext db)
                {
                        db.Database.ExecuteSqlRaw("""
                                CREATE TABLE IF NOT EXISTS MaintenanceHistories (
                                    Id BIGINT NOT NULL AUTO_INCREMENT,
                                    MaintenanceId BIGINT NOT NULL,
                                    ThietBi VARCHAR(100) NOT NULL,
                                    Loai VARCHAR(100) NOT NULL,
                                    NgayBt DATETIME(6) NOT NULL,
                                    LanTiepTheo DATETIME(6) NOT NULL,
                                    TrangThai VARCHAR(30) NOT NULL,
                                    NguoiThucHien VARCHAR(100) NOT NULL,
                                    GhiChu VARCHAR(500) NULL,
                                    GhiNhanLuc DATETIME(6) NOT NULL,
                                    PRIMARY KEY (Id),
                                    INDEX IX_MaintenanceHistories_ThietBi (ThietBi),
                                    INDEX IX_MaintenanceHistories_GhiNhanLuc (GhiNhanLuc)
                                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                                """);
                }

        private static void EnsureMachineHistoryColumns(ProductionDbContext db)
        {
#pragma warning disable EF1002
            var columns = new[]
            {
                (Name: "ThoiDiemKetThuc", Definition: "DATETIME(6) NULL AFTER ThoiDiem"),
                (Name: "ThoiLuongGiay", Definition: "INT NULL AFTER ThoiDiemKetThuc"),
                (Name: "LyDoDung", Definition: "VARCHAR(100) NULL AFTER Nguon"),
                (Name: "NguoiThucHien", Definition: "VARCHAR(100) NULL AFTER LyDoDung")
            };

            foreach (var column in columns)
            {
                var exists = db.Database
                    .SqlQueryRaw<int>($"SELECT COUNT(*) AS `Value` FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'MachineStatusHistory' AND COLUMN_NAME = '{column.Name}'")
                    .Single() > 0;
                if (!exists)
                {
                    db.Database.ExecuteSqlRaw($"ALTER TABLE MachineStatusHistory ADD COLUMN {column.Name} {column.Definition}");
                }
            }
#pragma warning restore EF1002
        }

        private static void EnsureMaintenanceColumns(ProductionDbContext db)
        {
            var performerExists = db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS `Value` FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'MaintenanceRecords' AND COLUMN_NAME = 'NguoiThucHien'")
                .Single() > 0;
            if (!performerExists)
            {
                db.Database.ExecuteSqlRaw("ALTER TABLE MaintenanceRecords ADD COLUMN NguoiThucHien VARCHAR(100) NOT NULL DEFAULT '' AFTER TrangThai");
            }

            const string legacyPrefix = "Người thực hiện: ";
            var legacyRecords = db.MaintenanceRecords
                .Where(item => item.NguoiThucHien == "" && item.GhiChu != null && item.GhiChu.StartsWith(legacyPrefix))
                .ToList();
            foreach (var item in legacyRecords)
            {
                var legacyValue = item.GhiChu![legacyPrefix.Length..];
                var separatorIndex = legacyValue.IndexOf(". ", StringComparison.Ordinal);
                if (separatorIndex < 0)
                {
                    item.NguoiThucHien = legacyValue;
                    item.GhiChu = null;
                }
                else
                {
                    item.NguoiThucHien = legacyValue[..separatorIndex];
                    item.GhiChu = legacyValue[(separatorIndex + 2)..];
                }
            }
            if (legacyRecords.Count > 0)
            {
                db.SaveChanges();
            }
        }

                private static void EnsureActionLogTable(ProductionDbContext db)
                {
                        db.Database.ExecuteSqlRaw("""
                                CREATE TABLE IF NOT EXISTS UserActionLogs (
                                    Id BIGINT NOT NULL AUTO_INCREMENT,
                                    UserId BIGINT NULL,
                                    Username VARCHAR(50) NOT NULL,
                                    Role VARCHAR(30) NOT NULL,
                                    ThoiDiem DATETIME(6) NOT NULL,
                                    LoaiThaoTac VARCHAR(50) NOT NULL,
                                    MaMay VARCHAR(50) NULL,
                                    MaIndex VARCHAR(50) NULL,
                                    NoiDung VARCHAR(500) NOT NULL,
                                    ThanhCong TINYINT(1) NOT NULL DEFAULT 0,
                                    Loi VARCHAR(500) NULL,
                                    PRIMARY KEY (Id),
                                    INDEX IX_UserActionLogs_ThoiDiem (ThoiDiem),
                                    INDEX IX_UserActionLogs_Username (Username),
                                    INDEX IX_UserActionLogs_MaMay (MaMay)
                                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                                """);
                }

        private static void SeedDevices(ProductionDbContext db)
        {
            UpsertDevice(db, "PLC", "PLC Gateway", "PLC", "Network", "Cổng giao tiếp PLC");
            UpsertDevice(db, "BT01", "Băng tải 01", "CONVEYOR", "Line 1", "Băng tải chính");
            UpsertDevice(db, "BT02", "Băng tải 02", "CONVEYOR", "Line 2", "Băng tải phụ");
            UpsertDevice(db, "CB02", "Cảm biến 02", "SENSOR", "Line 1", "Cảm biến vị trí");
            UpsertDevice(db, "M01", "Máy phân loại 01", "SORTER", "Line 1", "Máy phân loại sản phẩm");
        }

        private void EnsureAuthenticationSettings(ProductionDbContext db)
        {
            db.Database.ExecuteSqlRaw("""
                CREATE TABLE IF NOT EXISTS AuthenticationSettings (
                  Id INT NOT NULL,
                  AdminPin VARCHAR(6) NOT NULL,
                  UserPin VARCHAR(6) NOT NULL,
                  PRIMARY KEY (Id)
                ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci
                """);

            if (!db.AuthenticationSettings.Any(item => item.Id == 1))
            {
                db.AuthenticationSettings.Add(new AuthenticationSettingsRecord
                {
                    Id = 1,
                    AdminPin = configuration["Authentication:AdminPin"] ?? "582004",
                    UserPin = configuration["Authentication:UserPin"] ?? "123456"
                });
                db.SaveChanges();
            }
        }

        private static void UpsertDevice(ProductionDbContext db, string ma, string ten, string loai, string viTri, string moTa)
        {
            var existing = db.Devices.FirstOrDefault(item => item.Ma == ma);
            if (existing == null)
            {
                db.Devices.Add(new DeviceRecord { Ma = ma, Ten = ten, Loai = loai, ViTri = viTri, MoTa = moTa });
            }
        }

        private static void SeedMachines(ProductionDbContext db)
        {
            UpsertMachine(db, "GC01", "Máy gia công 01", "192.168.1.10", 502, "BT01");
            UpsertMachine(db, "GC02", "Máy gia công 02", "192.168.1.11", 502, "BT02");
            UpsertDevice(db, "GC01", "Máy gia công 01", "MACHINE", "Line 1", "Máy gia công");
            UpsertDevice(db, "GC02", "Máy gia công 02", "MACHINE", "Line 2", "Máy gia công");
        }

        private static void UpsertMachine(ProductionDbContext db, string ma, string ten, string ip, int port, string? bangTaiMap)
        {
            var existing = db.Machines.FirstOrDefault(item => item.MaMay == ma);
            if (existing == null)
            {
                db.Machines.Add(new MachineRecord
                {
                    MaMay = ma,
                    Ten = ten,
                    IpPlc = ip,
                    PortPlc = port,
                    TrangThai = "STOPPED",
                    IsActive = true,
                    BangTaiMap = bangTaiMap,
                    TrangThaiCapNhatLuc = DateTime.Now
                });
            }
        }

        private void SeedUsers(ProductionDbContext db)
        {
            EnsureUser(db, "admin", "admin123", "Admin");
            var legacyWorker = db.Users.FirstOrDefault(item => item.Username == "worker");
            var user = db.Users.FirstOrDefault(item => item.Username == "user");
            if (user == null && legacyWorker != null)
            {
                legacyWorker.Username = "user";
                legacyWorker.Role = "User";
                legacyWorker.PasswordHash = passwordHasher.HashPassword(legacyWorker, "user123");
                user = legacyWorker;
            }
            else
            {
                EnsureUser(db, "user", "user123", "User");
                user = db.Users.First(item => item.Username == "user");
            }
            user.Role = "User";
            if (legacyWorker != null && legacyWorker.Id != user.Id)
            {
                legacyWorker.IsActive = false;
            }
            db.SaveChanges();

            var supervisors = db.Users.Where(item => item.Role == "Supervisor").ToList();
            foreach (var supervisor in supervisors)
            {
                supervisor.IsActive = false;
            }

            if (!db.UserMachineAssignments.Any(item => item.UserId == user.Id && item.MaMay == "GC01"))
            {
                db.UserMachineAssignments.Add(new UserMachineAssignmentRecord { UserId = user.Id, MaMay = "GC01" });
            }
            if (!db.UserMachineAssignments.Any(item => item.UserId == user.Id && item.MaMay == "GC02"))
            {
                db.UserMachineAssignments.Add(new UserMachineAssignmentRecord { UserId = user.Id, MaMay = "GC02" });
            }
        }

        private void EnsureUser(ProductionDbContext db, string username, string password, string role)
        {
            var existing = db.Users.FirstOrDefault(item => item.Username == username);
            if (existing == null)
            {
                var user = new UserRecord
                {
                    Username = username,
                    Role = role,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                user.PasswordHash = passwordHasher.HashPassword(user, password);
                db.Users.Add(user);
            }
        }

        private static void SeedParts(ProductionDbContext db)
        {
            if (db.Parts.Any())
            {
                return;
            }

            db.Parts.AddRange(
                new PartRecord { MaIndex = "IDX-001", MaSp = "SP-BRG-01", TenChiTiet = "Bánh răng A", MaMay = "GC01", CreatedAt = DateTime.Now.AddMinutes(-2) },
                new PartRecord { MaIndex = "IDX-002", MaSp = "SP-BRG-02", TenChiTiet = "Vòng bi B", MaMay = "GC01", CreatedAt = DateTime.Now.AddMinutes(-1) },
                new PartRecord { MaIndex = "IDX-003", MaSp = "SP-BOLT-01", TenChiTiet = "Bu lông M8", MaMay = "GC02", CreatedAt = DateTime.Now });
        }
    }
}
