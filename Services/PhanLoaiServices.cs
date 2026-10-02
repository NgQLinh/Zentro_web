using System.Threading;
using Zentro.Models;

namespace Zentro.Services
{
    public class PhanLoaiServices
    {
        private readonly BangTaiModel trangThai;
        private readonly ProductionDatabaseService database;
        private readonly string deviceCode;
        private readonly SemaphoreSlim gate = new(1, 1);

        public PhanLoaiServices(BangTaiModel trangThai, ProductionDatabaseService database, string deviceCode = "BT01")
        {
            this.trangThai = trangThai;
            this.database = database;
            this.deviceCode = deviceCode;
        }

        public async Task PhanLoai(string loai)
        {
            if (trangThai.Mode == BangTaiMode.AUTO)
            {
                trangThai.TrangThai = "Đang AUTO, không dùng chức năng cảm biến tay";
                return;
            }

            await PhanLoaiInternal(loai, isAutoCycle: false);
        }

        public async Task PhanLoaiAuto(string loai)
        {
            await PhanLoaiInternal(loai, isAutoCycle: true);
        }

        private async Task PhanLoaiInternal(string loai, bool isAutoCycle)
        {
            await gate.WaitAsync();
            var cycleStartedAt = DateTime.Now;

            try
            {
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
                    trangThai.TrangThai = "ALARM đang kích hoạt";
                    trangThai.AddEvent("ALARM");
                    return;
                }

                if (!trangThai.DaReset)
                {
                    trangThai.State = BangTaiState.STOP;
                    trangThai.TrangThai = "Cần RESET trước khi chạy lại";
                    trangThai.AddEvent("RESET");
                    return;
                }

                if (trangThai.State == BangTaiState.PROCESS)
                {
                    trangThai.TrangThai = "Đang PROCESS, không cho chạy chồng lên";
                    return;
                }

                if (trangThai.SoSanPham >= trangThai.BufferMax)
                {
                    trangThai.BaoDay = true;
                    trangThai.MotorRunning = false;
                    trangThai.State = BangTaiState.STOP;
                    trangThai.TrangThai = $"Buffer đầy {trangThai.BufferMax} sản phẩm, tự dừng";
                    trangThai.AddEvent("Buffer đầy");
                    return;
                }

                trangThai.CamBien = true;
                trangThai.DangXuLy = true;
                trangThai.State = BangTaiState.PROCESS;
                trangThai.MotorRunning = false;
                trangThai.MotorDirection = "STOP";
                trangThai.AutoCycleStep = "PHÁT HIỆN";

                var loaiLinhKien = NormalizeType(loai);
                if (loaiLinhKien is not null)
                {
                    trangThai.AutoCycleStep = "NHẬN DIỆN";
                    trangThai.SoLuongTheoLoai[loaiLinhKien]++;
                    trangThai.SoSanPham++;
                    trangThai.TrangThai = $"Phát hiện {loaiLinhKien}";
                    trangThai.AddEvent($"Phát hiện {loaiLinhKien}");
                    await AddProductHistory(loaiLinhKien, "Đạt", cycleStartedAt);
                    trangThai.XyLanhA = loaiLinhKien == BangTaiModel.LoaiLinhKien[0];
                    trangThai.XyLanhB = loaiLinhKien == BangTaiModel.LoaiLinhKien[1];
                    trangThai.AutoCycleStep = "ĐẨY SẢN PHẨM";
                    await Task.Delay(700);
                    trangThai.XyLanhA = false;
                    trangThai.XyLanhB = false;
                }
                else
                {
                    trangThai.SoLoi++;
                    trangThai.SoSanPham++;
                    trangThai.TrangThai = "Sản phẩm lỗi";
                    trangThai.AddEvent("Sản phẩm lỗi");
                    await AddProductHistory("Lỗi", "Lỗi", cycleStartedAt);
                    trangThai.AutoCycleStep = "ĐẨY SẢN PHẨM LỖI";
                    trangThai.XyLanhA = false;
                    trangThai.XyLanhB = false;
                }

                if (trangThai.SoSanPham >= trangThai.BufferMax)
                {
                    trangThai.BaoDay = true;
                    trangThai.MotorRunning = false;
                    trangThai.State = BangTaiState.STOP;
                    trangThai.TrangThai = $"Buffer đầy {trangThai.BufferMax} sản phẩm, tự dừng";
                    trangThai.AddEvent("Buffer đầy");
                    return;
                }

                trangThai.CamBien = false;
                trangThai.DangXuLy = false;

                if (isAutoCycle && trangThai.Mode == BangTaiMode.AUTO)
                {
                    trangThai.MotorRunning = true;
                    trangThai.MotorDirection = "TIẾN";
                    trangThai.AutoTurning = true;
                    trangThai.State = BangTaiState.RUN;
                    trangThai.AutoCycleStep = "CHẠY BĂNG TẢI";
                    trangThai.TrangThai = "AUTO: băng tải chạy tiếp";
                }
                else
                {
                    trangThai.MotorRunning = false;
                    trangThai.MotorDirection = "STOP";
                    trangThai.AutoTurning = false;
                    trangThai.State = BangTaiState.MANUAL;
                    trangThai.AutoCycleStep = "MANUAL";
                    trangThai.TrangThai = "MANUAL: sẵn sàng cho thao tác tiếp theo";
                }

                trangThai.RecordCycle(DateTime.Now - cycleStartedAt);
            }
            finally
            {
                trangThai.CamBien = false;
                trangThai.DangXuLy = false;
                gate.Release();
            }
        }

        private async Task AddProductHistory(string loai, string ketQua, DateTime cycleStartedAt)
        {
            var now = DateTime.Now;
            var cycleTimeSeconds = (now - cycleStartedAt).TotalSeconds;
            trangThai.ProductHistory.Insert(0, new ProductHistoryItem
            {
                MaSp = $"SP{trangThai.SoSanPham:D3}",
                Loai = loai,
                KetQua = ketQua,
                ThoiGian = now.ToString("HH:mm:ss"),
                NgayGio = now,
                Ca = GetShift(now),
                Lo = trangThai.LoSanXuat,
                CycleTimeSeconds = cycleTimeSeconds
            });

            await database.SaveProductionAsync(new ProductionRecord
            {
                ThietBi = deviceCode,
                MaSp = $"SP{trangThai.SoSanPham:D3}",
                Loai = loai,
                KetQua = ketQua,
                NgayGio = now,
                Ca = GetShift(now),
                Lot = trangThai.LoSanXuat,
                CycleTimeSeconds = cycleTimeSeconds
            });

            if (trangThai.ProductHistory.Count > 20)
            {
                trangThai.ProductHistory.RemoveAt(trangThai.ProductHistory.Count - 1);
            }
        }

        private static string GetShift(DateTime timestamp)
        {
            return timestamp.Hour switch
            {
                >= 6 and < 14 => "CA 1",
                >= 14 and < 22 => "CA 2",
                _ => "CA 3"
            };
        }

        private static string? NormalizeType(string loai)
        {
            if (loai == "A") return BangTaiModel.LoaiLinhKien[0];
            if (loai == "B") return BangTaiModel.LoaiLinhKien[1];
            return BangTaiModel.LoaiLinhKien.FirstOrDefault(item => item.Equals(loai, StringComparison.OrdinalIgnoreCase));
        }
    }
}