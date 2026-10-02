CREATE DATABASE IF NOT EXISTS webnoibo CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE webnoibo;

CREATE TABLE IF NOT EXISTS ProductionLots (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  Lot VARCHAR(50) NOT NULL,
  MaSp VARCHAR(50) NOT NULL,
  SoLuongKeHoach INT NOT NULL DEFAULT 0,
  SoLuongThucTe INT NOT NULL DEFAULT 0,
  SoLuongOK INT NOT NULL DEFAULT 0,
  SoLuongNG INT NOT NULL DEFAULT 0,
  NgayBatDau DATETIME(6) NULL,
  NgayKetThuc DATETIME(6) NULL,
  TrangThai VARCHAR(30) NOT NULL DEFAULT 'WAITING',
  GhiChu VARCHAR(500) NULL,
  CONSTRAINT PK_ProductionLots PRIMARY KEY (Id),
  CONSTRAINT UQ_ProductionLots_Lot UNIQUE (Lot),
  INDEX IX_ProductionLots_MaSp (MaSp),
  INDEX IX_ProductionLots_TrangThai (TrangThai)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS Devices (
  Ma VARCHAR(50) NOT NULL,
  Ten VARCHAR(100) NOT NULL,
  Loai VARCHAR(50) NOT NULL,
  TrangThai VARCHAR(30) NOT NULL DEFAULT 'STOPPED',
  ViTri VARCHAR(100) NULL,
  MoTa VARCHAR(255) NULL,
  CONSTRAINT PK_Devices PRIMARY KEY (Ma),
  INDEX IX_Devices_Loai (Loai),
  INDEX IX_Devices_TrangThai (TrangThai)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS MachineSessions (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  Lot VARCHAR(50) NULL,
  CheDo VARCHAR(20) NOT NULL,
  ThoiGianBatDau DATETIME(6) NOT NULL,
  ThoiGianKetThuc DATETIME(6) NULL,
  TongCycle INT NOT NULL DEFAULT 0,
  SoLuongOK INT NOT NULL DEFAULT 0,
  SoLuongNG INT NOT NULL DEFAULT 0,
  TrangThai VARCHAR(30) NOT NULL DEFAULT 'RUNNING',
  CONSTRAINT PK_MachineSessions PRIMARY KEY (Id),
  CONSTRAINT FK_MachineSessions_ProductionLots FOREIGN KEY (Lot) REFERENCES ProductionLots(Lot) ON UPDATE CASCADE ON DELETE SET NULL,
  INDEX IX_MachineSessions_Lot (Lot),
  INDEX IX_MachineSessions_ThoiGianBatDau (ThoiGianBatDau)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS ProductionRecords (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  ThietBi VARCHAR(50) NOT NULL DEFAULT 'BT01',
  SessionId BIGINT NULL,
  MaSp VARCHAR(50) NOT NULL,
  Loai VARCHAR(50) NOT NULL,
  KetQua VARCHAR(20) NOT NULL,
  NgayGio DATETIME(6) NOT NULL,
  NgayGioKetThuc DATETIME(6) NULL,
  Ca VARCHAR(20) NOT NULL,
  Lot VARCHAR(50) NOT NULL,
  CycleTimeSeconds DOUBLE NOT NULL DEFAULT 0,
  CONSTRAINT PK_ProductionRecords PRIMARY KEY (Id),
  CONSTRAINT FK_ProductionRecords_ProductionLots FOREIGN KEY (Lot) REFERENCES ProductionLots(Lot) ON UPDATE CASCADE ON DELETE RESTRICT,
  CONSTRAINT FK_ProductionRecords_MachineSessions FOREIGN KEY (SessionId) REFERENCES MachineSessions(Id) ON UPDATE CASCADE ON DELETE SET NULL,
  INDEX IX_ProductionRecords_NgayGio (NgayGio),
  INDEX IX_ProductionRecords_Lot (Lot),
  INDEX IX_ProductionRecords_Ca (Ca),
  INDEX IX_ProductionRecords_KetQua (KetQua),
  INDEX IX_ProductionRecords_MaSp (MaSp)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS DeviceConfigs (
  ThietBi VARCHAR(50) NOT NULL,
  TocDoMacDinh INT NOT NULL DEFAULT 50,
  TocDoToiDa INT NOT NULL DEFAULT 100,
  DelayCamBien INT NOT NULL DEFAULT 2,
  TuDongKhoiDong TINYINT(1) NOT NULL DEFAULT 1,
  ChoPhepChay TINYINT(1) NOT NULL DEFAULT 1,
  ChuKyLuuSensor INT NOT NULL DEFAULT 10,
  ThoiGianCanhBao INT NOT NULL DEFAULT 0,
  CONSTRAINT PK_DeviceConfigs PRIMARY KEY (ThietBi)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

INSERT INTO DeviceConfigs (ThietBi) VALUES ('BT01'), ('BT02')
ON DUPLICATE KEY UPDATE ThietBi = VALUES(ThietBi);

SET @production_records_end_time_column_exists = (
  SELECT COUNT(*)
  FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ProductionRecords'
    AND COLUMN_NAME = 'NgayGioKetThuc'
);
SET @add_production_records_end_time_column = IF(
  @production_records_end_time_column_exists = 0,
  'ALTER TABLE ProductionRecords ADD COLUMN NgayGioKetThuc DATETIME(6) NULL AFTER NgayGio',
  'SELECT 1'
);
PREPARE add_production_records_end_time_column FROM @add_production_records_end_time_column;
EXECUTE add_production_records_end_time_column;
DEALLOCATE PREPARE add_production_records_end_time_column;

CREATE TABLE IF NOT EXISTS Sensors (
  Ma VARCHAR(50) NOT NULL,
  Ten VARCHAR(100) NOT NULL,
  Loai VARCHAR(50) NOT NULL,
  TrangThai VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',
  DonVi VARCHAR(20) NULL,
  MoTa VARCHAR(255) NULL,
  CONSTRAINT PK_Sensors PRIMARY KEY (Ma),
  INDEX IX_Sensors_Loai (Loai),
  INDEX IX_Sensors_TrangThai (TrangThai)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS SensorData (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  MaSensor VARCHAR(50) NOT NULL,
  GiaTri DOUBLE NOT NULL,
  NgayGio DATETIME(6) NOT NULL,
  CONSTRAINT PK_SensorData PRIMARY KEY (Id),
  CONSTRAINT FK_SensorData_Sensors FOREIGN KEY (MaSensor) REFERENCES Sensors(Ma) ON UPDATE CASCADE ON DELETE CASCADE,
  INDEX IX_SensorData_MaSensor (MaSensor),
  INDEX IX_SensorData_NgayGio (NgayGio)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS AlarmRecords (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  MucDo VARCHAR(20) NOT NULL,
  ThietBi VARCHAR(100) NOT NULL,
  NoiDung VARCHAR(500) NOT NULL,
  NgayGio DATETIME(6) NOT NULL,
  NgayGioKetThuc DATETIME(6) NULL,
  DurationSeconds INT NOT NULL DEFAULT 0,
  TrangThai VARCHAR(30) NOT NULL DEFAULT 'ACTIVE',
  CONSTRAINT PK_AlarmRecords PRIMARY KEY (Id),
  CONSTRAINT FK_AlarmRecords_Devices FOREIGN KEY (ThietBi) REFERENCES Devices(Ma) ON UPDATE CASCADE ON DELETE RESTRICT,
  INDEX IX_AlarmRecords_NgayGio (NgayGio),
  INDEX IX_AlarmRecords_MucDo (MucDo),
  INDEX IX_AlarmRecords_ThietBi (ThietBi),
  INDEX IX_AlarmRecords_TrangThai (TrangThai)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS EventRecords (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  NgayGio DATETIME(6) NOT NULL,
  NoiDung VARCHAR(500) NOT NULL,
  LoaiSuKien VARCHAR(50) NULL,
  ThietBi VARCHAR(100) NULL,
  CheDo VARCHAR(20) NULL,
  CONSTRAINT PK_EventRecords PRIMARY KEY (Id),
  CONSTRAINT FK_EventRecords_Devices FOREIGN KEY (ThietBi) REFERENCES Devices(Ma) ON UPDATE CASCADE ON DELETE SET NULL,
  INDEX IX_EventRecords_NgayGio (NgayGio),
  INDEX IX_EventRecords_LoaiSuKien (LoaiSuKien),
  INDEX IX_EventRecords_ThietBi (ThietBi)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS MaintenanceRecords (
  Id BIGINT NOT NULL AUTO_INCREMENT,
  ThietBi VARCHAR(100) NOT NULL,
  Loai VARCHAR(50) NOT NULL,
  NgayBt DATETIME(6) NOT NULL,
  LanTiepTheo DATETIME(6) NOT NULL,
  TrangThai VARCHAR(30) NOT NULL DEFAULT 'PLANNED',
  NguoiThucHien VARCHAR(100) NOT NULL DEFAULT '',
  GhiChu VARCHAR(500) NULL,
  CONSTRAINT PK_MaintenanceRecords PRIMARY KEY (Id),
  CONSTRAINT FK_MaintenanceRecords_Devices FOREIGN KEY (ThietBi) REFERENCES Devices(Ma) ON UPDATE CASCADE ON DELETE RESTRICT,
  INDEX IX_MaintenanceRecords_ThietBi (ThietBi),
  INDEX IX_MaintenanceRecords_NgayBt (NgayBt),
  INDEX IX_MaintenanceRecords_LanTiepTheo (LanTiepTheo),
  INDEX IX_MaintenanceRecords_TrangThai (TrangThai)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

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
  CONSTRAINT PK_MaintenanceHistories PRIMARY KEY (Id),
  INDEX IX_MaintenanceHistories_ThietBi (ThietBi),
  INDEX IX_MaintenanceHistories_GhiNhanLuc (GhiNhanLuc)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS SystemConfigs (
  Id INT NOT NULL,
  TocDoMacDinh INT NOT NULL DEFAULT 50,
  TocDoToiDa INT NOT NULL DEFAULT 100,
  DelayCamBien INT NOT NULL DEFAULT 2,
  TuDongKhoiDong TINYINT(1) NOT NULL DEFAULT 1,
  ChoPhepChay TINYINT(1) NOT NULL DEFAULT 1,
  ChuKyLuuSensor INT NOT NULL DEFAULT 10,
  ThoiGianCanhBao INT NOT NULL DEFAULT 0,
  CONSTRAINT PK_SystemConfigs PRIMARY KEY (Id)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

INSERT INTO SystemConfigs (Id) VALUES (1) ON DUPLICATE KEY UPDATE Id = Id;

INSERT INTO ProductionLots (Lot, MaSp, SoLuongKeHoach, TrangThai) VALUES
  ('L020260818', 'LINH KIỆN MÁY MÓC', 1000, 'RUNNING')
ON DUPLICATE KEY UPDATE MaSp = VALUES(MaSp), SoLuongKeHoach = VALUES(SoLuongKeHoach), TrangThai = VALUES(TrangThai);

INSERT INTO Devices (Ma, Ten, Loai, ViTri, MoTa) VALUES
  ('BT01', 'Băng tải 01', 'CONVEYOR', 'Line 1', 'Băng tải chính'),
  ('BT02', 'Băng tải 02', 'CONVEYOR', 'Line 2', 'Băng tải phụ'),
  ('CB02', 'Cảm biến 02', 'SENSOR', 'Line 1', 'Cảm biến vị trí'),
  ('M01', 'Máy phân loại 01', 'SORTER', 'Line 1', 'Máy phân loại sản phẩm')
ON DUPLICATE KEY UPDATE Ten = VALUES(Ten), Loai = VALUES(Loai), ViTri = VALUES(ViTri), MoTa = VALUES(MoTa);

INSERT INTO Sensors (Ma, Ten, Loai, TrangThai, MoTa) VALUES
  ('CB01', 'Phát hiện sản phẩm', 'Quang', 'ACTIVE', 'Phát hiện sản phẩm đầu vào'),
  ('CB02', 'Vị trí', 'Quang', 'ACTIVE', 'Phát hiện vị trí sản phẩm'),
  ('CB03', 'Nhận diện loại sản phẩm', 'Phân loại', 'ACTIVE', 'Phân loại Bánh răng, Vòng bi, Bu lông, Đai ốc, Trục máy'),
  ('CB04', 'Cuối băng tải', 'Quang', 'ACTIVE', 'Phát hiện sản phẩm cuối băng tải'),
  ('CB05', 'Nhiệt độ', 'Nhiệt', 'ACTIVE', 'Đo nhiệt độ hệ thống'),
  ('CB06', 'Trọng lượng', 'Cân', 'ACTIVE', 'Đo trọng lượng sản phẩm')
ON DUPLICATE KEY UPDATE Ten = VALUES(Ten), Loai = VALUES(Loai), TrangThai = VALUES(TrangThai), MoTa = VALUES(MoTa);

SHOW TABLES;

SELECT
  TABLE_NAME AS BangCon,
  COLUMN_NAME AS CotLienKet,
  CONSTRAINT_NAME AS TenKhoaNgoai,
  REFERENCED_TABLE_NAME AS BangCha,
  REFERENCED_COLUMN_NAME AS CotCha
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME, CONSTRAINT_NAME;
