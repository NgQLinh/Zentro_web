using Microsoft.EntityFrameworkCore;
using Zentro.Models;

namespace Zentro.Data
{
    public class ProductionDbContext : DbContext
    {
        public ProductionDbContext(DbContextOptions<ProductionDbContext> options) : base(options)
        {
        }

        public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
        public DbSet<DeviceConfigRecord> DeviceConfigs => Set<DeviceConfigRecord>();
        public DbSet<ProductionLotRecord> ProductionLots => Set<ProductionLotRecord>();
        public DbSet<DeviceRecord> Devices => Set<DeviceRecord>();
        public DbSet<MachineSessionRecord> MachineSessions => Set<MachineSessionRecord>();
        public DbSet<SensorDataRecord> SensorData => Set<SensorDataRecord>();
        public DbSet<AlarmRecord> AlarmRecords => Set<AlarmRecord>();
        public DbSet<EventRecord> EventRecords => Set<EventRecord>();
        public DbSet<SensorRecord> Sensors => Set<SensorRecord>();
        public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
        public DbSet<MaintenanceHistoryRecord> MaintenanceHistories => Set<MaintenanceHistoryRecord>();
        public DbSet<SystemConfigRecord> SystemConfigs => Set<SystemConfigRecord>();
        public DbSet<MachineRecord> Machines => Set<MachineRecord>();
        public DbSet<MachineStatusHistoryRecord> MachineStatusHistory => Set<MachineStatusHistoryRecord>();
        public DbSet<PartRecord> Parts => Set<PartRecord>();
        public DbSet<MachiningRecord> MachiningRecords => Set<MachiningRecord>();
        public DbSet<PlcDataRecord> PLCData => Set<PlcDataRecord>();
        public DbSet<UserRecord> Users => Set<UserRecord>();
        public DbSet<UserMachineAssignmentRecord> UserMachineAssignments => Set<UserMachineAssignmentRecord>();
        public DbSet<UserActionLogRecord> UserActionLogs => Set<UserActionLogRecord>();
        public DbSet<AuthenticationSettingsRecord> AuthenticationSettings => Set<AuthenticationSettingsRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductionRecord>().HasIndex(item => item.NgayGio);
            modelBuilder.Entity<ProductionRecord>().HasIndex(item => item.ThietBi);
            modelBuilder.Entity<ProductionRecord>().HasIndex(item => item.Lot);
            modelBuilder.Entity<ProductionRecord>().HasIndex(item => item.Ca);
            modelBuilder.Entity<ProductionRecord>().HasOne<ProductionLotRecord>().WithMany().HasForeignKey(item => item.Lot).HasPrincipalKey(item => item.Lot).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProductionRecord>().HasOne<MachineSessionRecord>().WithMany().HasForeignKey(item => item.SessionId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<AlarmRecord>().HasIndex(item => item.NgayGio);
            modelBuilder.Entity<AlarmRecord>().HasIndex(item => item.MucDo);
            modelBuilder.Entity<EventRecord>().HasIndex(item => item.NgayGio);
            modelBuilder.Entity<SensorRecord>().HasKey(item => item.Ma);
            modelBuilder.Entity<DeviceRecord>().HasKey(item => item.Ma);
            modelBuilder.Entity<SensorDataRecord>().HasOne<SensorRecord>().WithMany().HasForeignKey(item => item.MaSensor).HasPrincipalKey(item => item.Ma).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MaintenanceRecord>().HasIndex(item => item.ThietBi);
            modelBuilder.Entity<MaintenanceRecord>().HasOne<DeviceRecord>().WithMany().HasForeignKey(item => item.ThietBi).HasPrincipalKey(item => item.Ma).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AlarmRecord>().HasOne<DeviceRecord>().WithMany().HasForeignKey(item => item.ThietBi).HasPrincipalKey(item => item.Ma).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EventRecord>().HasOne<DeviceRecord>().WithMany().HasForeignKey(item => item.ThietBi).HasPrincipalKey(item => item.Ma).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MachineSessionRecord>().HasOne<ProductionLotRecord>().WithMany().HasForeignKey(item => item.Lot).HasPrincipalKey(item => item.Lot).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<SystemConfigRecord>().HasKey(item => item.Id);
            modelBuilder.Entity<DeviceConfigRecord>().HasKey(item => item.ThietBi);
            modelBuilder.Entity<AlarmRecord>().Property(item => item.NoiDung).HasMaxLength(500);
            modelBuilder.Entity<EventRecord>().Property(item => item.NoiDung).HasMaxLength(500);
            modelBuilder.Entity<MaintenanceRecord>().Property(item => item.GhiChu).HasMaxLength(500);
            modelBuilder.Entity<MaintenanceHistoryRecord>().HasIndex(item => item.ThietBi);
            modelBuilder.Entity<MaintenanceHistoryRecord>().HasIndex(item => item.GhiNhanLuc);

            modelBuilder.Entity<MachineRecord>().HasKey(item => item.MaMay);
            modelBuilder.Entity<MachineRecord>().HasIndex(item => item.TrangThai);
            modelBuilder.Entity<MachineStatusHistoryRecord>().HasIndex(item => item.MaMay);
            modelBuilder.Entity<MachineStatusHistoryRecord>().HasIndex(item => item.ThoiDiem);
            modelBuilder.Entity<MachineStatusHistoryRecord>().Property(item => item.LyDoDung).HasMaxLength(100);
            modelBuilder.Entity<MachineStatusHistoryRecord>().Property(item => item.NguoiThucHien).HasMaxLength(100);
            modelBuilder.Entity<MachineStatusHistoryRecord>().HasOne<MachineRecord>().WithMany().HasForeignKey(item => item.MaMay).HasPrincipalKey(item => item.MaMay).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PartRecord>().HasIndex(item => item.MaIndex);
            modelBuilder.Entity<PartRecord>().HasIndex(item => item.MaSp);
            modelBuilder.Entity<PartRecord>().HasIndex(item => item.CreatedAt);
            modelBuilder.Entity<MachiningRecord>().HasIndex(item => item.MaMay);
            modelBuilder.Entity<MachiningRecord>().HasIndex(item => item.ThoiGianBatDau);
            modelBuilder.Entity<MachiningRecord>().HasIndex(item => item.MaSp);
            modelBuilder.Entity<MachiningRecord>().HasIndex(item => item.TrangThai);
            modelBuilder.Entity<MachiningRecord>().HasOne<MachineRecord>().WithMany().HasForeignKey(item => item.MaMay).HasPrincipalKey(item => item.MaMay).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MachiningRecord>().HasOne<PartRecord>().WithMany().HasForeignKey(item => item.PartId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MachiningRecord>().HasOne<UserRecord>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PlcDataRecord>().ToTable("PLCData");
            modelBuilder.Entity<PlcDataRecord>().HasIndex(item => item.MaMay);
            modelBuilder.Entity<PlcDataRecord>().HasIndex(item => item.NgayGio);
            modelBuilder.Entity<PlcDataRecord>().HasOne<MachineRecord>().WithMany().HasForeignKey(item => item.MaMay).HasPrincipalKey(item => item.MaMay).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserRecord>().HasIndex(item => item.Username).IsUnique();
            modelBuilder.Entity<UserMachineAssignmentRecord>().HasIndex(item => new { item.UserId, item.MaMay }).IsUnique();
            modelBuilder.Entity<UserMachineAssignmentRecord>().HasOne<UserRecord>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserMachineAssignmentRecord>().HasOne<MachineRecord>().WithMany().HasForeignKey(item => item.MaMay).HasPrincipalKey(item => item.MaMay).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<UserActionLogRecord>().HasIndex(item => item.ThoiDiem);
            modelBuilder.Entity<UserActionLogRecord>().HasIndex(item => item.Username);
            modelBuilder.Entity<UserActionLogRecord>().HasIndex(item => item.MaMay);
            modelBuilder.Entity<UserActionLogRecord>().Property(item => item.NoiDung).HasMaxLength(500);
            modelBuilder.Entity<UserActionLogRecord>().Property(item => item.Loi).HasMaxLength(500);
            modelBuilder.Entity<AuthenticationSettingsRecord>().HasKey(item => item.Id);
        }
    }
}
