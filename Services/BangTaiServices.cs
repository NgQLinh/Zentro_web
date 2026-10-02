using Zentro.Models;

namespace Zentro.Services
{
    public class BangTaiServices
    {
        private readonly BangTaiModel trangThai;
        private readonly PhanLoaiServices phanLoaiServices;
        private readonly ProductionDatabaseService database;
        private readonly NhaMayDataService nhaMayData;
        private readonly string deviceCode;
        private CancellationTokenSource? autoLoopCts;

        public BangTaiServices(BangTaiModel trangThai, PhanLoaiServices phanLoaiServices, ProductionDatabaseService database, NhaMayDataService nhaMayData, string deviceCode = "BT01")
        {
            this.trangThai = trangThai;
            this.phanLoaiServices = phanLoaiServices;
            this.database = database;
            this.nhaMayData = nhaMayData;
            this.deviceCode = deviceCode;
        }

        public void SetModeAuto()
        {
            if (!CanRun()) return;
            if (trangThai.DungKhanCap)
            {
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "E-STOP đang kích hoạt";
                trangThai.AddEvent("E-STOP");
                return;
            }

            if (trangThai.LoiMay || trangThai.BaoLoi)
            {
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "Đang ALARM, không cho chuyển AUTO";
                trangThai.AddEvent("ALARM");
                return;
            }

            if (!trangThai.DaReset)
            {
                trangThai.State = BangTaiState.STOP;
                trangThai.TrangThai = "Chưa RESET, chưa thể chạy AUTO";
                trangThai.AddEvent("RESET");
                return;
            }

            StopAutoLoop();
            trangThai.Mode = BangTaiMode.AUTO;
            trangThai.State = BangTaiState.RUN;
            trangThai.MotorRunning = true;
            trangThai.MotorDirection = "TIẾN";
            trangThai.AutoTurning = true;
            trangThai.AutoCycleStep = "CHẠY BĂNG TẢI";
            trangThai.TrangThai = "AUTO: máy đang chạy chu trình";
            trangThai.AddEvent("AUTO");
            StartAutoLoop();
        }

        public void StartIfConfigured()
        {
            if (nhaMayData.GetConfig(deviceCode).TuDongKhoiDong)
            {
                Start();
            }
        }

        public void SetModeManual()
        {
            StopAutoLoop();
            trangThai.Mode = BangTaiMode.MANUAL;
            trangThai.State = BangTaiState.MANUAL;
            trangThai.MotorRunning = false;
            trangThai.MotorDirection = "STOP";
            trangThai.AutoTurning = false;
            trangThai.AutoCycleStep = "MANUAL";
            trangThai.XyLanhA = false;
            trangThai.XyLanhB = false;
            trangThai.TrangThai = "MANUAL: bấm từng thiết bị để test";
            trangThai.AddEvent("MANUAL");
        }

        public void Start()
        {
            if (!CanRun()) return;
            if (trangThai.DungKhanCap)
            {
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "E-STOP đang kích hoạt";
                trangThai.AddEvent("E-STOP");
                return;
            }

            if (trangThai.LoiMay || trangThai.BaoLoi)
            {
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "Đang ALARM, không cho START";
                trangThai.AddEvent("ALARM");
                return;
            }

            if (!trangThai.DaReset)
            {
                trangThai.State = BangTaiState.STOP;
                trangThai.TrangThai = "Chưa RESET, không chạy lại";
                trangThai.AddEvent("RESET");
                return;
            }

            if (trangThai.State == BangTaiState.PROCESS)
            {
                trangThai.TrangThai = "Đang PROCESS, không cho chạy chồng lên";
                return;
            }

            if (trangThai.BaoDay)
            {
                trangThai.State = BangTaiState.STOP;
                trangThai.TrangThai = "Buffer đầy, hệ thống dừng";
                trangThai.AddEvent("Buffer đầy");
                return;
            }

            if (trangThai.Mode == BangTaiMode.AUTO && autoLoopCts != null)
            {
                trangThai.TrangThai = "Chu trình AUTO đang chạy";
                return;
            }

            trangThai.Mode = BangTaiMode.AUTO;
            trangThai.State = BangTaiState.RUN;
            trangThai.MotorRunning = true;
            trangThai.MotorDirection = "TIẾN";
            trangThai.AutoTurning = true;
            trangThai.AutoCycleStep = "CHẠY BĂNG TẢI";
            trangThai.TrangThai = "AUTO: máy đang chạy chu trình";
            trangThai.AddEvent("START");
            StartAutoLoop();
        }

        public void Stop()
        {
            StopAutoLoop();
            trangThai.Mode = BangTaiMode.MANUAL;
            trangThai.MotorRunning = false;
            trangThai.MotorDirection = "STOP";
            trangThai.AutoTurning = false;
            trangThai.AutoCycleStep = "IDLE";
            trangThai.XyLanhA = false;
            trangThai.XyLanhB = false;
            trangThai.State = BangTaiState.STOP;
            trangThai.TrangThai = "Băng tải đã dừng";
            trangThai.AddEvent("STOP");
        }

        public async Task MoveManual(string direction)
        {
            if (trangThai.DungKhanCap || trangThai.LoiMay || !trangThai.DaReset)
            {
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "Không thể điều khiển motor khi ALARM/E-STOP";
                return;
            }

            StopAutoLoop();
            trangThai.Mode = BangTaiMode.MANUAL;
            trangThai.State = BangTaiState.MANUAL;
            trangThai.MotorRunning = true;
            trangThai.MotorDirection = direction == "LUI" ? "LÙI" : "TIẾN";
            trangThai.AutoTurning = false;
            trangThai.TrangThai = $"MANUAL: motor đang chạy {direction.ToLowerInvariant()}";
            trangThai.AddEvent($"MOTOR {direction}");

            var loai = direction == "LUI"
                ? BangTaiModel.LoaiLinhKien[1]
                : BangTaiModel.LoaiLinhKien[0];
            await phanLoaiServices.PhanLoai(loai);
            trangThai.MotorRunning = true;
            trangThai.MotorDirection = direction == "LUI" ? "LÙI" : "TIẾN";
            trangThai.AutoCycleStep = "MANUAL ĐÃ GHI NHẬN";
            trangThai.TrangThai = $"MANUAL: đã ghi nhận sản phẩm {loai}";
        }

        public void EmergencyStop()
        {
            StopAutoLoop();
            trangThai.DungKhanCap = true;
            trangThai.MotorRunning = false;
            trangThai.MotorDirection = "STOP";
            trangThai.AutoTurning = false;
            trangThai.AutoCycleStep = "E-STOP";
            trangThai.XyLanhA = false;
            trangThai.XyLanhB = false;
            trangThai.State = BangTaiState.ALARM;
            trangThai.TrangThai = "E-STOP kích hoạt: Motor OFF và xy lanh OFF";
            trangThai.AddEvent("E-STOP");
            SaveAlarm("ERROR", deviceCode, "Dừng khẩn cấp kích hoạt");
        }

        public void BaoLoiMay()
        {
            StopAutoLoop();
            trangThai.LoiMay = true;
            trangThai.BaoLoi = true;
            trangThai.MotorRunning = false;
            trangThai.MotorDirection = "STOP";
            trangThai.AutoTurning = false;
            trangThai.AutoCycleStep = "ALARM";
            trangThai.XyLanhA = false;
            trangThai.XyLanhB = false;
            trangThai.State = BangTaiState.ALARM;
            trangThai.TrangThai = "ALARM: lỗi máy";
            trangThai.AddEvent("ALARM");
            SaveAlarm("ERROR", deviceCode, "Lỗi máy");
        }

        public void Reset()
        {
            StopAutoLoop();
            trangThai.DungKhanCap = false;
            trangThai.LoiMay = false;
            trangThai.BaoLoi = false;
            trangThai.BaoDay = false;
            trangThai.DangXuLy = false;
            trangThai.CamBien = false;
            trangThai.MotorRunning = false;
            trangThai.MotorDirection = "STOP";
            trangThai.AutoTurning = false;
            trangThai.AutoCycleStep = "IDLE";
            trangThai.XyLanhA = false;
            trangThai.XyLanhB = false;
            trangThai.State = BangTaiState.STOP;
            trangThai.Mode = BangTaiMode.MANUAL;
            trangThai.DaReset = true;
            trangThai.SoSanPham = 0;
            foreach (var loai in BangTaiModel.LoaiLinhKien)
            {
                trangThai.SoLuongTheoLoai[loai] = 0;
            }
            trangThai.SoLoi = 0;
            trangThai.ProductHistory.Clear();
            trangThai.TrangThai = "Hệ thống đã RESET";
            trangThai.AddEvent("RESET");
        }

        public void SetSpeed(int speed)
        {
            trangThai.TocDo = Math.Clamp(speed, 0, nhaMayData.GetConfig(deviceCode).TocDoToiDa);
            trangThai.AddEvent($"Đặt tốc độ {trangThai.TocDo}%");
        }

        public void SetCylinder(string cylinder, bool active)
        {
            if (trangThai.DungKhanCap || trangThai.LoiMay || !trangThai.DaReset)
            {
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "Không thể thao tác xy lanh khi ALARM/E-STOP";
                return;
            }

            if (cylinder == "A")
            {
                trangThai.XyLanhA = active;
                if (active)
                {
                    trangThai.AddEvent("Lỗi xy lanh");
                }
            }
            else if (cylinder == "B")
            {
                trangThai.XyLanhB = active;
                if (active)
                {
                    trangThai.AddEvent("Lỗi xy lanh");
                }
            }
        }

        private void SaveAlarm(string level, string equipment, string message)
        {
            database.SaveAlarm(new AlarmRecord
            {
                MucDo = level,
                ThietBi = equipment,
                NoiDung = message,
                NgayGio = DateTime.Now,
                DurationSeconds = 0,
                TrangThai = "CHƯA XỬ LÝ"
            });
        }

        private void StartAutoLoop()
        {
            StopAutoLoop();
            autoLoopCts = new CancellationTokenSource();
            _ = Task.Run(() => RunAutoLoopAsync(autoLoopCts.Token));
        }

        private void StopAutoLoop()
        {
            autoLoopCts?.Cancel();
            autoLoopCts?.Dispose();
            autoLoopCts = null;
        }

        private async Task RunAutoLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested &&
                       trangThai.Mode == BangTaiMode.AUTO &&
                       !trangThai.DungKhanCap &&
                       !trangThai.LoiMay &&
                       trangThai.DaReset &&
                       !trangThai.BaoDay)
                {
                    if (trangThai.State == BangTaiState.PROCESS)
                    {
                        await Task.Delay(200, cancellationToken);
                        continue;
                    }

                    var loai = BangTaiModel.LoaiLinhKien[Random.Shared.Next(BangTaiModel.LoaiLinhKien.Count)];
                    trangThai.AutoCycleStep = "PHÁT HIỆN";
                    await phanLoaiServices.PhanLoaiAuto(loai);
                    await Task.Delay(TimeSpan.FromSeconds(nhaMayData.GetConfig(deviceCode).DelayCamBien), cancellationToken);

                    if (trangThai.SoSanPham >= trangThai.BufferMax)
                    {
                        trangThai.BaoDay = true;
                        trangThai.MotorRunning = false;
                        trangThai.State = BangTaiState.STOP;
                        trangThai.TrangThai = $"Buffer đầy {trangThai.BufferMax} sản phẩm, tự dừng";
                        trangThai.AddEvent("Buffer đầy");
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Dừng vòng lặp theo lệnh STOP/RESET là trạng thái bình thường.
            }
            catch (Exception)
            {
                trangThai.MotorRunning = false;
                trangThai.AutoTurning = false;
                trangThai.State = BangTaiState.ALARM;
                trangThai.TrangThai = "AUTO dừng do lỗi xử lý. Kiểm tra kết nối cơ sở dữ liệu.";
            }
        }

        private bool CanRun()
        {
            if (nhaMayData.GetConfig(deviceCode).ChoPhepChay)
            {
                return true;
            }

            trangThai.State = BangTaiState.STOP;
            trangThai.MotorRunning = false;
            trangThai.TrangThai = "Hệ thống đang bị khóa bởi cấu hình Cho phép chạy.";
            trangThai.AddEvent("CHẶN CHẠY BỞI CẤU HÌNH");
            return false;
        }
    }
}
