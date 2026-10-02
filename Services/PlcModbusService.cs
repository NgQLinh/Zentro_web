using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Zentro.Data;
using Zentro.Hubs;
using Zentro.Models;

namespace Zentro.Services
{
    public class PlcOptions
    {
        public bool SimulationMode { get; set; } = true;
        public int PollIntervalSeconds { get; set; } = 3;
        public int SaveDataIntervalSeconds { get; set; } = 30;
        public double TemperatureAlarmCelsius { get; set; } = 80;
        public PlcRegisterOptions Registers { get; set; } = new();
    }

    public class PlcRegisterOptions
    {
        public ushort Status { get; set; } = 0;
        public ushort Sensor { get; set; } = 1;
        public ushort Temperature { get; set; } = 2;
        public ushort Alarm { get; set; } = 3;
    }

    public class PlcRuntimeState
    {
        private int simulationMode = 1;

        public Dictionary<string, PlcMachineSnapshot> Machines { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool SimulationMode
        {
            get => Volatile.Read(ref simulationMode) != 0;
            set => Volatile.Write(ref simulationMode, value ? 1 : 0);
        }
    }

    public class PlcMachineSnapshot
    {
        public string MaMay { get; set; } = string.Empty;
        public bool Online { get; set; }
        public int StatusCode { get; set; }
        public double Sensor { get; set; }
        public double Temperature { get; set; }
        public bool Alarm { get; set; }
        public DateTime LastCommunication { get; set; }
        public string? LastError { get; set; }
    }

    public class PlcModbusService : BackgroundService
    {
        private readonly IDbContextFactory<ProductionDbContext> contextFactory;
        private readonly ProductionDatabaseService database;
        private readonly MachineService machineService;
        private readonly BangTaiModel bangTai;
        private readonly BangTaiLine2Model bangTai2;
        private readonly PlcOptions options;
        private readonly PlcRuntimeState runtimeState;
        private readonly IHubContext<DashboardHub> dashboardHub;
        private readonly Dictionary<string, bool?> previousOnline = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> lastSaved = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random random = new();

        public PlcModbusService(
            IDbContextFactory<ProductionDbContext> contextFactory,
            ProductionDatabaseService database,
            MachineService machineService,
            BangTaiModel bangTai,
            BangTaiLine2Model bangTai2,
            IOptions<PlcOptions> options,
            PlcRuntimeState runtimeState,
            IHubContext<DashboardHub> dashboardHub)
        {
            this.contextFactory = contextFactory;
            this.database = database;
            this.machineService = machineService;
            this.bangTai = bangTai;
            this.bangTai2 = bangTai2;
            this.options = options.Value;
            this.runtimeState = runtimeState;
            this.runtimeState.SimulationMode = this.options.SimulationMode;
            this.dashboardHub = dashboardHub;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAllAsync(stoppingToken);
                    await PublishSnapshotAsync(stoppingToken);
                }
                catch
                {
                    // Keep service alive on transient errors.
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds)), stoppingToken);
            }
        }

        private async Task PollAllAsync(CancellationToken cancellationToken)
        {
            var simulationMode = runtimeState.SimulationMode;
            List<MachineRecord> machines;
            await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
            {
                machines = await db.Machines.AsNoTracking()
                    .Where(item => item.IsActive)
                    .ToListAsync(cancellationToken);
            }

            foreach (var machine in machines)
            {
                var snapshot = simulationMode
                    ? Simulate(machine)
                    : await ReadModbusAsync(machine, cancellationToken);

                runtimeState.Machines[machine.MaMay] = snapshot;
                machineService.SetPlcOnline(machine.MaMay, snapshot.Online);
                HandleConnectionChange(machine, snapshot);
                ApplyStatusFromPlc(machine, snapshot, simulationMode);
                BridgeToConveyor(machine, snapshot);
                MaybeSavePlcData(machine.MaMay, snapshot);
                CheckTemperatureAlarm(machine.MaMay, snapshot);
            }

            bangTai.PlcOnline = runtimeState.Machines.Values.Any(item => item.Online)
                || (simulationMode && machines.Count > 0);
            if (bangTai.PlcOnline)
            {
                bangTai.PlcLastCommunication = DateTime.Now;
            }
        }

        private Task PublishSnapshotAsync(CancellationToken cancellationToken)
        {
            var machines = runtimeState.Machines.Values.Select(snapshot => new
            {
                snapshot.MaMay,
                snapshot.Online,
                snapshot.StatusCode,
                snapshot.Sensor,
                snapshot.Temperature,
                snapshot.Alarm,
                snapshot.LastCommunication,
                snapshot.LastError
            }).ToList();

            return dashboardHub.Clients.All.SendAsync("plcSnapshotUpdated", new
            {
                isConnected = bangTai.PlcOnline,
                lastCommunication = bangTai.PlcLastCommunication,
                machines
            }, cancellationToken);
        }

        private PlcMachineSnapshot Simulate(MachineRecord machine)
        {
            var existing = runtimeState.Machines.TryGetValue(machine.MaMay, out var current) ? current : null;
            var statusCode = machine.TrangThai switch
            {
                "RUNNING" => 1,
                "PAUSED" => 2,
                "ALARM" => 4,
                "MAINTENANCE" => 5,
                _ => 0
            };

            return new PlcMachineSnapshot
            {
                MaMay = machine.MaMay,
                Online = true,
                StatusCode = statusCode,
                Sensor = existing?.Sensor > 0 ? existing.Sensor : random.Next(0, 2),
                Temperature = 40 + random.NextDouble() * 15,
                Alarm = machine.TrangThai == "ALARM",
                LastCommunication = DateTime.Now
            };
        }

        private async Task<PlcMachineSnapshot> ReadModbusAsync(MachineRecord machine, CancellationToken cancellationToken)
        {
            var snapshot = new PlcMachineSnapshot
            {
                MaMay = machine.MaMay,
                LastCommunication = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(machine.IpPlc))
            {
                snapshot.Online = false;
                snapshot.LastError = "Chưa cấu hình IP PLC";
                return snapshot;
            }

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(machine.IpPlc, machine.PortPlc <= 0 ? 502 : machine.PortPlc);
                var completed = await Task.WhenAny(connectTask, Task.Delay(1500, cancellationToken));
                if (completed != connectTask || !client.Connected)
                {
                    snapshot.Online = false;
                    snapshot.LastError = "Không kết nối được PLC";
                    return snapshot;
                }

                snapshot.Online = true;
                snapshot.StatusCode = await ReadHoldingRegisterAsync(client, options.Registers.Status, cancellationToken);
                snapshot.Sensor = await ReadHoldingRegisterAsync(client, options.Registers.Sensor, cancellationToken);
                snapshot.Temperature = await ReadHoldingRegisterAsync(client, options.Registers.Temperature, cancellationToken);
                snapshot.Alarm = await ReadHoldingRegisterAsync(client, options.Registers.Alarm, cancellationToken) != 0;
            }
            catch (Exception ex)
            {
                snapshot.Online = false;
                snapshot.LastError = ex.Message;
            }

            return snapshot;
        }

        private static async Task<int> ReadHoldingRegisterAsync(TcpClient client, ushort address, CancellationToken cancellationToken)
        {
            var transactionId = (ushort)Random.Shared.Next(1, ushort.MaxValue);
            var request = new byte[]
            {
                (byte)(transactionId >> 8), (byte)transactionId, 0, 0, 0, 6,
                1, 3, (byte)(address >> 8), (byte)address, 0, 1
            };

            var stream = client.GetStream();
            stream.ReadTimeout = 1500;
            stream.WriteTimeout = 1500;
            await stream.WriteAsync(request, cancellationToken);
            var response = new byte[11];
            await stream.ReadExactlyAsync(response, cancellationToken);

            if (response[0] != request[0] || response[1] != request[1] || response[7] != 3 || response[8] != 2)
            {
                throw new IOException($"Phản hồi Modbus không hợp lệ tại thanh ghi {address}.");
            }

            return (response[9] << 8) | response[10];
        }

        private void HandleConnectionChange(MachineRecord machine, PlcMachineSnapshot snapshot)
        {
            previousOnline.TryGetValue(machine.MaMay, out var previous);
            if (previous.HasValue && previous.Value != snapshot.Online)
            {
                database.SaveAlarm(new AlarmRecord
                {
                    MucDo = snapshot.Online ? "INFO" : "ERROR",
                    ThietBi = machine.MaMay,
                    NoiDung = snapshot.Online
                        ? $"PLC {machine.MaMay} kết nối lại"
                        : $"Mất kết nối PLC {machine.MaMay} ({machine.IpPlc}:{machine.PortPlc})",
                    NgayGio = DateTime.Now,
                    TrangThai = "CHƯA XỬ LÝ",
                    LoaiCanhBao = "PLC_CONNECTION"
                });
                database.SaveEvent(new EventRecord
                {
                    NgayGio = DateTime.Now,
                    NoiDung = snapshot.Online ? $"PLC {machine.MaMay} online" : $"PLC {machine.MaMay} offline",
                    LoaiSuKien = "PLC",
                    ThietBi = machine.MaMay
                });
            }

            previousOnline[machine.MaMay] = snapshot.Online;
        }

        private void ApplyStatusFromPlc(MachineRecord machine, PlcMachineSnapshot snapshot, bool simulationMode)
        {
            if (!snapshot.Online || simulationMode)
            {
                return;
            }

            if (snapshot.Alarm)
            {
                machineService.SetStatus(machine.MaMay, "ALARM", "PLC", "Tín hiệu alarm từ PLC");
                return;
            }

            var mapped = snapshot.StatusCode switch
            {
                1 => "RUNNING",
                2 => "PAUSED",
                4 => "ALARM",
                5 => "MAINTENANCE",
                _ => "STOPPED"
            };
            machineService.SetStatus(machine.MaMay, mapped, "PLC");
        }

        private void BridgeToConveyor(MachineRecord machine, PlcMachineSnapshot snapshot)
        {
            if (string.Equals(machine.BangTaiMap, "BT01", StringComparison.OrdinalIgnoreCase))
            {
                bangTai.CamBien = snapshot.Sensor > 0;
                bangTai.NhietDo = snapshot.Temperature;
                if (snapshot.Alarm)
                {
                    bangTai.LoiMay = true;
                }
            }
            else if (string.Equals(machine.BangTaiMap, "BT02", StringComparison.OrdinalIgnoreCase))
            {
                bangTai2.CamBien = snapshot.Sensor > 0;
                bangTai2.NhietDo = snapshot.Temperature;
                if (snapshot.Alarm)
                {
                    bangTai2.LoiMay = true;
                }
            }
        }

        private void MaybeSavePlcData(string maMay, PlcMachineSnapshot snapshot)
        {
            if (!snapshot.Online)
            {
                return;
            }

            var now = DateTime.Now;
            if (lastSaved.TryGetValue(maMay, out var last) &&
                (now - last).TotalSeconds < Math.Max(5, options.SaveDataIntervalSeconds))
            {
                return;
            }

            lastSaved[maMay] = now;
            using var db = contextFactory.CreateDbContext();
            db.PLCData.AddRange(
                new PlcDataRecord { MaMay = maMay, Tag = "STATUS", Address = options.Registers.Status.ToString(), GiaTri = snapshot.StatusCode, NgayGio = now },
                new PlcDataRecord { MaMay = maMay, Tag = "SENSOR", Address = options.Registers.Sensor.ToString(), GiaTri = snapshot.Sensor, NgayGio = now },
                new PlcDataRecord { MaMay = maMay, Tag = "TEMP", Address = options.Registers.Temperature.ToString(), GiaTri = snapshot.Temperature, NgayGio = now },
                new PlcDataRecord { MaMay = maMay, Tag = "ALARM", Address = options.Registers.Alarm.ToString(), GiaTri = snapshot.Alarm ? 1 : 0, NgayGio = now });
            db.SaveChanges();
        }

        private void CheckTemperatureAlarm(string maMay, PlcMachineSnapshot snapshot)
        {
            if (!snapshot.Online || snapshot.Temperature < options.TemperatureAlarmCelsius)
            {
                return;
            }

            using var db = contextFactory.CreateDbContext();
            var recent = db.AlarmRecords.Any(item =>
                item.ThietBi == maMay &&
                item.LoaiCanhBao == "TEMPERATURE" &&
                item.TrangThai == "CHƯA XỬ LÝ" &&
                item.NgayGio > DateTime.Now.AddMinutes(-10));
            if (recent)
            {
                return;
            }

            database.SaveAlarm(new AlarmRecord
            {
                MucDo = "WARN",
                ThietBi = maMay,
                NoiDung = $"Cảnh báo nhiệt độ PLC {maMay}: {snapshot.Temperature:0.0}°C",
                NgayGio = DateTime.Now,
                TrangThai = "CHƯA XỬ LÝ",
                LoaiCanhBao = "TEMPERATURE"
            });
        }
    }
}
