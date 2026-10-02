namespace Zentro.Models
{
    public enum BangTaiMode
    {
        AUTO,
        MANUAL
    }

    public enum BangTaiState
    {
        STOP,
        RUN,
        PROCESS,
        ALARM,
        MANUAL
    }

    public class EventLogItem
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Message { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
    }

    public class BangTaiModel
    {
        public bool DangChay { get; set; }
        public bool CamBien { get; set; }
        public bool DangXuLy { get; set; }
        public bool BaoDay { get; set; }
        public bool DungKhanCap { get; set; }
        public bool LoiMay { get; set; }
        public bool BaoLoi { get; set; }
        public bool DaReset { get; set; } = true;
        public const int DefaultBufferMax = 1000;
        public static readonly IReadOnlyList<string> LoaiLinhKien = new[]
        {
            "Bánh răng",
            "Vòng bi",
            "Bu lông",
            "Đai ốc",
            "Trục máy"
        };

        public int BufferMax { get; set; } = DefaultBufferMax;

        public int SoSanPham { get; set; }
        public Dictionary<string, int> SoLuongTheoLoai { get; set; } = LoaiLinhKien.ToDictionary(loai => loai, _ => 0);
        public int SoLoi { get; set; }

        public int TongSanPhamDat => SoLuongTheoLoai.Values.Sum();
        public int SoLoaiA => SoLuongTheoLoai.GetValueOrDefault(LoaiLinhKien[0]);
        public int SoLoaiB => SoLuongTheoLoai.GetValueOrDefault(LoaiLinhKien[1]);

        public bool XyLanhA { get; set; }
        public bool XyLanhB { get; set; }
        private bool motorRunning;
        private DateTime motorStateChangedAt = DateTime.Now;
        private TimeSpan accumulatedRunTime;
        private TimeSpan accumulatedStopTime;
        public bool MotorRunning
        {
            get => motorRunning;
            set
            {
                if (motorRunning == value)
                {
                    return;
                }

                var elapsed = DateTime.Now - motorStateChangedAt;
                if (motorRunning)
                {
                    accumulatedRunTime += elapsed;
                }
                else
                {
                    accumulatedStopTime += elapsed;
                }

                motorRunning = value;
                motorStateChangedAt = DateTime.Now;
            }
        }
        public TimeSpan RunTime => accumulatedRunTime + (motorRunning ? DateTime.Now - motorStateChangedAt : TimeSpan.Zero);
        public TimeSpan StopTime => accumulatedStopTime + (!motorRunning ? DateTime.Now - motorStateChangedAt : TimeSpan.Zero);
        public int CycleCount { get; private set; }
        public double TotalCycleSeconds { get; private set; }
        public double AverageCycleTimeSeconds => CycleCount == 0 ? 0 : TotalCycleSeconds / CycleCount;

        public void RecordCycle(TimeSpan duration)
        {
            CycleCount++;
            TotalCycleSeconds += duration.TotalSeconds;
        }
        
        public string MotorDirection { get; set; } = "STOP";
        public bool AutoTurning { get; set; }
        public string AutoCycleStep { get; set; } = "IDLE";

        public BangTaiMode Mode { get; set; } = BangTaiMode.MANUAL;
        public BangTaiState State { get; set; } = BangTaiState.STOP;

        public string TrangThai { get; set; } = "Hệ thống đang dừng";
        public List<EventLogItem> EventLogs { get; set; } = new();
        public List<ProductHistoryItem> ProductHistory { get; set; } = new();
        public Action<EventLogItem>? EventAdded { get; set; }

        public int TocDo { get; set; } = 50;
        public int TocDoToiDa { get; set; } = 100;
        public double NhietDo { get; set; } = 42;
        public bool PlcOnline { get; set; } = true;
        public DateTime? PlcLastCommunication { get; set; } = DateTime.Now;
        public int KeHoach { get; set; } = 1000;
        public string LoSanXuat { get; set; } = "L020260818";
        public string SanPhamDangSx { get; set; } = "LINH KIỆN MÁY MÓC";

        public string TrangThaiMotor => MotorRunning ? "RUNNING" : State == BangTaiState.PROCESS ? "PROCESS" : "STOPPED";
        public string CheDoHienThi => Mode == BangTaiMode.AUTO ? "AUTO" : "MANUAL";
        public int ConLai => Math.Max(0, KeHoach - SoSanPham);

        public void AddEvent(string message)
        {
            var eventItem = new EventLogItem
            {
                Timestamp = DateTime.Now,
                Message = message
            };
            EventLogs.Insert(0, eventItem);
            EventAdded?.Invoke(eventItem);

            if (EventLogs.Count > 50)
            {
                EventLogs.RemoveAt(EventLogs.Count - 1);
            }
        }
    }

    public class BangTaiLine2Model : BangTaiModel
    {
    }
}


